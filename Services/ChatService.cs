using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LANChatPro.Models;
using LANChatPro.Network;
using LANChatPro.Storage;
using LANChatPro.Utils;

namespace LANChatPro.Services
{
    public class ChatService
    {
        public ConfigManager ConfigManager { get; }
        public ChatHistoryService HistoryService { get; }
        public NotificationService NotificationService { get; }
        public FileTransferService FileTransferService { get; }

        public string LocalIp { get; }
        public string MyId => $"{LocalIp}:{ConfigManager.Config.Port}";

        public ConcurrentDictionary<string, PeerInfo> DiscoveredPeers { get; } = new();

        // Core business events
        public event Action<PeerInfo>? PeerOnline;
        public event Action<PeerInfo>? PeerOffline;
        public event Action<PeerInfo>? PeerUpdated;
        public event Action<ChatMessage>? GroupMessageReceived;
        public event Action<string, ChatMessage>? PrivateMessageReceived; // peerId, message
        public event Action<PeerInfo, bool>? PeerTypingChanged; // peer, isTyping
        public event Action<NetworkMessage, string>? FileRequestReceived; // message, senderIp

        private UdpDiscoveryService? _udpDiscovery;
        private TcpServerService? _tcpServer;
        private System.Threading.Timer? _peerTimeoutTimer;

        public ChatService()
        {
            ConfigManager = new ConfigManager();
            HistoryService = new ChatHistoryService();
            NotificationService = new NotificationService(ConfigManager);
            FileTransferService = new FileTransferService();

            LocalIp = Helpers.GetLocalIPAddress();

            // Port scanning strategy to support multiple clients running on the same computer
            int port = ConfigManager.Config.Port;
            while (!IsPortAvailable(port))
            {
                port++;
            }
            ConfigManager.Config.Port = port;
        }

        private bool IsPortAvailable(int port)
        {
            try
            {
                using var client = new TcpListener(System.Net.IPAddress.Any, port);
                client.Start();
                client.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Start()
        {
            if (_tcpServer != null || _udpDiscovery != null)
                return;

            // Bind TCP listener server
            _tcpServer = new TcpServerService(ConfigManager.Config.Port);
            _tcpServer.MessageReceived += HandleTcpMessage;
            _tcpServer.Start();

            // Bind UDP broadcast discovery socket
            _udpDiscovery = new UdpDiscoveryService(LocalIp, ConfigManager.Config.Port, CreateNetworkMessage);
            _udpDiscovery.MessageReceived += HandleUdpMessage;
            _udpDiscovery.Start();

            // Start peer dead-node sweeping timer (every 3 seconds)
            _peerTimeoutTimer = new System.Threading.Timer(EvaluatePeerTimeouts, null, 3000, 3000);

            Logger.Info($"ChatService running at local IP: {LocalIp}, TCP Port: {ConfigManager.Config.Port}. Unique Node ID: {MyId}");
        }

        public void Stop()
        {
            _peerTimeoutTimer?.Dispose();
            _peerTimeoutTimer = null;

            _udpDiscovery?.Stop();
            _udpDiscovery = null;

            _tcpServer?.Stop();
            _tcpServer = null;

            Logger.Info("ChatService stopped.");
        }

        public NetworkMessage CreateNetworkMessage()
        {
            return new NetworkMessage
            {
                SenderId = MyId,
                SenderUsername = ConfigManager.Config.Username,
                SenderMachineName = Environment.MachineName,
                SenderPort = ConfigManager.Config.Port,
                SenderAvatarIndex = ConfigManager.Config.AvatarIndex
            };
        }

        // --- Core Chat Send Functions ---

        public async Task<bool> SendGroupMessageAsync(string text)
        {
            var chatMsg = new ChatMessage
            {
                SenderId = MyId,
                SenderUsername = ConfigManager.Config.Username,
                Text = text,
                IsPrivate = false,
                Timestamp = DateTime.UtcNow
            };

            HistoryService.AddGroupMessage(chatMsg);

            var netMsg = CreateNetworkMessage();
            netMsg.Type = "CHAT";
            netMsg.Content = text;
            netMsg.IsPrivate = false;

            // Broadcast TCP packet to all discovered peers
            var tasks = DiscoveredPeers.Values
                .Where(p => p.Status != "offline")
                .Select(peer => TcpClientService.SendMessageAsync(peer.IpAddress, peer.Port, netMsg))
                .ToArray();

            bool[] results = await Task.WhenAll(tasks);
            return results.Length == 0 || results.All(result => result);
        }

        public async Task<bool> SendPrivateMessageAsync(string peerId, string text)
        {
            if (!DiscoveredPeers.TryGetValue(peerId, out var peer))
                return false;

            var chatMsg = new ChatMessage
            {
                SenderId = MyId,
                SenderUsername = ConfigManager.Config.Username,
                Text = text,
                IsPrivate = true,
                RecipientId = peerId,
                Timestamp = DateTime.UtcNow
            };

            HistoryService.AddPrivateMessage(peerId, chatMsg);

            var netMsg = CreateNetworkMessage();
            netMsg.Type = "CHAT";
            netMsg.Content = text;
            netMsg.IsPrivate = true;
            netMsg.RecipientId = peerId;

            return await TcpClientService.SendMessageAsync(peer.IpAddress, peer.Port, netMsg);
        }

        public async Task SendTypingStateAsync(string? peerId, bool isTyping)
        {
            var netMsg = CreateNetworkMessage();
            netMsg.Type = "TYPING";
            netMsg.Content = isTyping.ToString();

            if (string.IsNullOrEmpty(peerId))
            {
                // Send typing indicators to all active peers
                var tasks = DiscoveredPeers.Values
                    .Where(p => p.Status != "offline")
                    .Select(peer => TcpClientService.SendMessageAsync(peer.IpAddress, peer.Port, netMsg));
                await Task.WhenAll(tasks);
            }
            else
            {
                // Send to direct private peer
                if (DiscoveredPeers.TryGetValue(peerId, out var peer))
                {
                    await TcpClientService.SendMessageAsync(peer.IpAddress, peer.Port, netMsg);
                }
            }
        }

        public async Task<bool> SendFileRequestAsync(string peerId, string filePath, int dynamicFilePort, string transferId)
        {
            if (!DiscoveredPeers.TryGetValue(peerId, out var peer))
                return false;

            var fileInfo = new FileInfo(filePath);
            var netMsg = CreateNetworkMessage();
            netMsg.Type = "FILE_REQ";
            netMsg.FileId = transferId;
            netMsg.FileName = fileInfo.Name;
            netMsg.FileSize = fileInfo.Length;
            netMsg.FilePort = dynamicFilePort; // Tell the receiver to connect to this dynamic port.

            return await TcpClientService.SendMessageAsync(peer.IpAddress, peer.Port, netMsg);
        }

        public async Task SendFileRejectAsync(string peerIp, int peerPort, string transferId)
        {
            var netMsg = CreateNetworkMessage();
            netMsg.Type = "FILE_REJECT";
            netMsg.FileId = transferId;

            await TcpClientService.SendMessageAsync(peerIp, peerPort, netMsg);
        }

        // --- Core Message Dispatchers ---

        private void HandleUdpMessage(NetworkMessage msg, string senderIp)
        {
            if (msg.SenderId == MyId)
                return; // Skip loops from our own broadcasts

            if (msg.Type == "GOODBYE")
            {
                if (DiscoveredPeers.TryGetValue(msg.SenderId, out var leavingPeer))
                {
                    bool changed;
                    lock (leavingPeer)
                    {
                        UpdatePeerFromMessage(leavingPeer, msg, senderIp, DateTime.UtcNow);
                        changed = leavingPeer.Status != "offline";
                        leavingPeer.Status = "offline";
                        leavingPeer.IsTyping = false;
                    }

                    if (changed)
                    {
                        PeerOffline?.Invoke(leavingPeer);
                        NotificationService.ShowToast($"{leavingPeer.Username} went Offline", "User closed the application.");
                    }
                }

                return;
            }

            bool isNew = false;
            bool wentOnline = false;
            var now = DateTime.UtcNow;

            if (!DiscoveredPeers.TryGetValue(msg.SenderId, out var peer))
            {
                peer = new PeerInfo { Id = msg.SenderId };
                isNew = DiscoveredPeers.TryAdd(msg.SenderId, peer);
                if (!isNew && !DiscoveredPeers.TryGetValue(msg.SenderId, out peer))
                    return;
            }

            lock (peer)
            {
                UpdatePeerFromMessage(peer, msg, senderIp, now);
                if (peer.Status == "offline")
                {
                    wentOnline = true;
                }

                peer.Status = "online";
            }

            if (isNew)
            {
                PeerOnline?.Invoke(peer);
                NotificationService.PlayOnlineSound();
                NotificationService.ShowToast($"{peer.Username} is Online", "A new peer joined the local network.");
                
                // Immediately reply with a broadcast so they see us without waiting 5 seconds
                _udpDiscovery?.BroadcastHello();
            }
            else if (wentOnline)
            {
                PeerOnline?.Invoke(peer);
                NotificationService.PlayOnlineSound();
                NotificationService.ShowToast($"{peer.Username} is Online", "Peer returned online.");
            }
            else
            {
                PeerUpdated?.Invoke(peer);
            }

        }

        private static void UpdatePeerFromMessage(PeerInfo peer, NetworkMessage msg, string senderIp, DateTime lastSeen)
        {
            peer.LastSeen = lastSeen;
            peer.Username = string.IsNullOrWhiteSpace(msg.SenderUsername) ? peer.Username : msg.SenderUsername;
            peer.MachineName = string.IsNullOrWhiteSpace(msg.SenderMachineName) ? peer.MachineName : msg.SenderMachineName;
            peer.IpAddress = senderIp;
            if (msg.SenderPort > 0)
            {
                peer.Port = msg.SenderPort;
            }
            peer.AvatarIndex = msg.SenderAvatarIndex;
        }

        private void HandleTcpMessage(NetworkMessage msg, string senderIp)
        {
            if (msg.SenderId == MyId)
                return;

            if (msg.IsPrivate && !string.Equals(msg.RecipientId, MyId, StringComparison.Ordinal))
                return;

            bool wasKnown = DiscoveredPeers.ContainsKey(msg.SenderId);
            PeerInfo peer = TouchPeerFromTcpMessage(msg, senderIp);

            if (!wasKnown || peer.Status == "offline")
            {
                peer.Status = "online";
                PeerOnline?.Invoke(peer);
            }
            else
            {
                PeerUpdated?.Invoke(peer);
            }

            switch (msg.Type)
            {
                case "CHAT":
                    var chatMsg = new ChatMessage
                    {
                        SenderId = msg.SenderId,
                        SenderUsername = msg.SenderUsername,
                        Text = msg.Content,
                        IsPrivate = msg.IsPrivate,
                        RecipientId = msg.RecipientId,
                        Timestamp = DateTime.UtcNow
                    };

                    if (msg.IsPrivate)
                    {
                        HistoryService.AddPrivateMessage(msg.SenderId, chatMsg);
                        NotificationService.PlayMessageSound();
                        NotificationService.ShowToast($"Message from {msg.SenderUsername}", msg.Content);
                        PrivateMessageReceived?.Invoke(msg.SenderId, chatMsg);
                    }
                    else
                    {
                        HistoryService.AddGroupMessage(chatMsg);
                        NotificationService.PlayMessageSound();
                        NotificationService.ShowToast($"# Kênh chung - {msg.SenderUsername}", msg.Content);
                        GroupMessageReceived?.Invoke(chatMsg);
                    }
                    break;

                case "TYPING":
                    bool.TryParse(msg.Content, out bool isTyping);
                    peer.IsTyping = isTyping;
                    peer.TypingStartTime = isTyping ? DateTime.UtcNow : DateTime.MinValue;
                    PeerTypingChanged?.Invoke(peer, isTyping);
                    break;

                case "FILE_REQ":
                    FileRequestReceived?.Invoke(msg, senderIp);
                    break;

                case "FILE_REJECT":
                    FileTransferService.RejectSendSession(msg.FileId);
                    NotificationService.ShowToast("File Rejected", $"{msg.SenderUsername} declined your file transfer.");
                    break;
            }
        }

        private PeerInfo TouchPeerFromTcpMessage(NetworkMessage msg, string senderIp)
        {
            var now = DateTime.UtcNow;
            if (!DiscoveredPeers.TryGetValue(msg.SenderId, out var peer))
            {
                peer = new PeerInfo { Id = msg.SenderId };
                if (!DiscoveredPeers.TryAdd(msg.SenderId, peer) &&
                    !DiscoveredPeers.TryGetValue(msg.SenderId, out peer))
                {
                    throw new InvalidOperationException("Unable to register peer from TCP message.");
                }
            }

            lock (peer)
            {
                UpdatePeerFromMessage(peer, msg, senderIp, now);
            }

            return peer;
        }

        private void EvaluatePeerTimeouts(object? state)
        {
            var now = DateTime.UtcNow;
            foreach (var peer in DiscoveredPeers.Values)
            {
                if (peer.Status != "offline")
                {
                    // Dead node offline detection: 15 seconds silent timeout threshold
                    if ((now - peer.LastSeen).TotalSeconds >= 15)
                    {
                        peer.Status = "offline";
                        peer.IsTyping = false;
                        PeerOffline?.Invoke(peer);
                        NotificationService.ShowToast($"{peer.Username} went Offline", "User timed out on local network.");
                    }
                    // Autoclear typing indicators after 4 seconds
                    else if (peer.IsTyping && (now - peer.TypingStartTime).TotalSeconds >= 4)
                    {
                        peer.IsTyping = false;
                        PeerTypingChanged?.Invoke(peer, false);
                    }
                }
            }
        }
    }
}
