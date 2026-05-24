using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        public string MyId => ConfigManager.Config.ClientId;

        public ConcurrentDictionary<string, PeerInfo> DiscoveredPeers { get; } = new();
        public ConcurrentDictionary<string, PendingFileDownload> PendingFileDownloads { get; } = new();

public event Action<PeerInfo>? PeerOnline;
        public event Action<PeerInfo>? PeerOffline;
        public event Action<PeerInfo>? PeerUpdated;
        public event Action<ChatMessage>? GroupMessageReceived;
        public event Action<string, ChatMessage>? PrivateMessageReceived;
        public event Action<PeerInfo, bool>? PeerTypingChanged;
        public event Action? ServerDisconnected;
        public event Action? HistorySynced;

        private ServerConnection? _serverConnection;
        private System.Threading.Timer? _peerTimeoutTimer;
        private System.Threading.Timer? _serverHeartbeatTimer;
        private int _heartbeatInFlight;

        public ChatService()
        {
            ConfigManager = new ConfigManager();
            HistoryService = new ChatHistoryService();
            NotificationService = new NotificationService(ConfigManager);
            FileTransferService = new FileTransferService();

            LocalIp = Helpers.GetLocalIPAddress();

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

        public static async Task<bool> CanConnectToServerAsync(string serverIp, int serverPort, int timeoutMs = 3000)
        {
            try
            {
                using var client = new TcpClient();
                using var cts = new CancellationTokenSource(timeoutMs);
                await client.ConnectAsync(serverIp, serverPort, cts.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> StartAsync()
        {
            if (_serverConnection != null)
                return true;

            var serverConnection = new ServerConnection(ConfigManager.Config.ServerIp, ConfigManager.Config.ServerPort);
            serverConnection.MessageReceived += HandleServerMessage;
            serverConnection.Disconnected += ServerConnection_Disconnected;

            var joinMsg = CreateNetworkMessage();
            joinMsg.Type = "JOIN";
            bool connected = await serverConnection.ConnectAsync(joinMsg);
            if (!connected)
            {
                serverConnection.MessageReceived -= HandleServerMessage;
                serverConnection.Disconnected -= ServerConnection_Disconnected;
                serverConnection.Disconnect();
                return false;
            }

            _serverConnection = serverConnection;

            _peerTimeoutTimer = new System.Threading.Timer(EvaluatePeerTimeouts, null, 3000, 3000);
            _serverHeartbeatTimer = new System.Threading.Timer(SendServerHeartbeat, null, 5000, 5000);

            Logger.Info($"ChatService running at local IP: {LocalIp}, TCP Port: {ConfigManager.Config.Port}. Connected to Server: {ConfigManager.Config.ServerIp}:{ConfigManager.Config.ServerPort}");
            return true;
        }

        private void ServerConnection_Disconnected()
        {
            var connection = _serverConnection;
            if (connection == null)
                return;

            connection.MessageReceived -= HandleServerMessage;
            connection.Disconnected -= ServerConnection_Disconnected;
            _serverConnection = null;

            _peerTimeoutTimer?.Dispose();
            _peerTimeoutTimer = null;
            _serverHeartbeatTimer?.Dispose();
            _serverHeartbeatTimer = null;

            ServerDisconnected?.Invoke();
        }

        private void SendServerHeartbeat(object? state)
        {
            if (Interlocked.Exchange(ref _heartbeatInFlight, 1) == 1)
                return;

            _ = Task.Run(async () =>
            {
                try
                {
                    var connection = _serverConnection;
                    if (connection == null)
                        return;

                    var heartbeat = CreateNetworkMessage();
                    heartbeat.Type = "HEARTBEAT";
                    await connection.SendMessageAsync(heartbeat);
                }
                finally
                {
                    Interlocked.Exchange(ref _heartbeatInFlight, 0);
                }
            });
        }

        private void MarkPeerOffline(string peerId, string reason)
        {
            if (!DiscoveredPeers.TryGetValue(peerId, out var peer))
                return;

            bool changed;
            lock (peer)
            {
                changed = peer.Status != "offline";
                peer.Status = "offline";
                peer.IsTyping = false;
            }

            if (changed)
            {
                PeerOffline?.Invoke(peer);
                NotificationService.ShowToast($"{peer.Username} went Offline", reason);
            }
        }

        public void Stop()
        {
            _peerTimeoutTimer?.Dispose();
            _peerTimeoutTimer = null;

            _serverHeartbeatTimer?.Dispose();
            _serverHeartbeatTimer = null;

            var connection = _serverConnection;
            _serverConnection = null;
            if (connection != null)
            {
                connection.MessageReceived -= HandleServerMessage;
                connection.Disconnected -= ServerConnection_Disconnected;
                connection.Disconnect();
            }

            Logger.Info("ChatService stopped.");
        }

        public async Task<bool> RestartNetworkAsync()
        {
            Stop();
            DiscoveredPeers.Clear();
            PendingFileDownloads.Clear();
            return await StartAsync();
        }

        public NetworkMessage CreateNetworkMessage()
        {
            return new NetworkMessage
            {
                SenderId = MyId,
                SenderUsername = ConfigManager.Config.Username,
                SenderMachineName = Environment.MachineName,
                SenderIp = LocalIp,
                SenderPort = ConfigManager.Config.Port,
                SenderAvatarIndex = ConfigManager.Config.AvatarIndex
            };
        }

        private static string GetPeerTransportIp(PeerInfo peer)
        {
            return string.Equals(peer.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                ? "127.0.0.1"
                : peer.IpAddress;
        }

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
            netMsg.Id = chatMsg.Id;
            netMsg.Timestamp = chatMsg.Timestamp;
            netMsg.Type = "CHAT";
            netMsg.Content = text;
            netMsg.IsPrivate = false;

            if (_serverConnection == null) return false;
            return await _serverConnection.SendMessageAsync(netMsg);
        }

        public async Task<bool> SendGroupFileRequestAsync(string filePath, string transferId)
        {
            var fileInfo = new FileInfo(filePath);

            var chatMsg = new ChatMessage
            {
                Id = transferId,
                SenderId = MyId,
                SenderUsername = ConfigManager.Config.Username,
                Text = $"Đã gửi file: {fileInfo.Name}",
                FileId = transferId,
                IsPrivate = false,
                FilePath = filePath,
                FileSize = fileInfo.Length,
                Timestamp = DateTime.UtcNow
            };
            HistoryService.AddGroupMessage(chatMsg);

            var netMsg = CreateNetworkMessage();
            netMsg.Id = chatMsg.Id;
            netMsg.Timestamp = chatMsg.Timestamp;
            netMsg.Type = "CHAT";
            netMsg.Content = $"Đã chia sẻ file: {fileInfo.Name}";
            netMsg.IsPrivate = false;
            netMsg.FileId = transferId;
            netMsg.FileName = fileInfo.Name;
            netMsg.FileSize = fileInfo.Length;

            if (_serverConnection == null) return false;
            return await _serverConnection.SendMessageAsync(netMsg);
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
            netMsg.Id = chatMsg.Id;
            netMsg.Timestamp = chatMsg.Timestamp;
            netMsg.Type = "CHAT";
            netMsg.Content = text;
            netMsg.IsPrivate = true;
            netMsg.RecipientId = peerId;

            if (_serverConnection == null) return false;
            return await _serverConnection.SendMessageAsync(netMsg);
        }

        public async Task SendTypingStateAsync(string? peerId, bool isTyping)
        {
            var netMsg = CreateNetworkMessage();
            netMsg.Type = "TYPING";
            netMsg.Content = isTyping.ToString();

            if (_serverConnection != null)
            {
                if (!string.IsNullOrEmpty(peerId)) netMsg.RecipientId = peerId;
                await _serverConnection.SendMessageAsync(netMsg);
            }
        }

        public async Task<bool> SendFileRequestAsync(string peerId, string filePath, string transferId)
        {
            if (!DiscoveredPeers.TryGetValue(peerId, out var peer))
                return false;

            var fileInfo = new FileInfo(filePath);
            var chatMsg = new ChatMessage
            {
                Id = transferId,
                SenderId = MyId,
                SenderUsername = ConfigManager.Config.Username,
                Text = $"Đã gửi file: {fileInfo.Name}",
                FileId = transferId,
                IsPrivate = true,
                RecipientId = peerId,
                FilePath = filePath,
                FileSize = fileInfo.Length,
                Timestamp = DateTime.UtcNow
            };
            HistoryService.AddPrivateMessage(peerId, chatMsg);

            var netMsg = CreateNetworkMessage();
            netMsg.Id = chatMsg.Id;
            netMsg.Timestamp = chatMsg.Timestamp;
            netMsg.Type = "CHAT";
            netMsg.Content = $"Đã chia sẻ file: {fileInfo.Name}";
            netMsg.IsPrivate = true;
            netMsg.RecipientId = peerId;
            netMsg.FileId = transferId;
            netMsg.FileName = fileInfo.Name;
            netMsg.FileSize = fileInfo.Length;

            if (_serverConnection == null) return false;
            return await _serverConnection.SendMessageAsync(netMsg);
        }

        public bool TryGetPendingFileDownload(string fileId, out PendingFileDownload? pendingFile)
        {
            return PendingFileDownloads.TryGetValue(fileId, out pendingFile);
        }

        public bool StartPendingFileDownload(string fileId, string savePath)
        {
            // Legacy stub - P2P download no longer used. Download is server-buffered via StartDownloadSessionAsync.
            return PendingFileDownloads.ContainsKey(fileId);
        }

        public async Task SendFileRejectAsync(string peerId, string transferId)
        {
            var netMsg = CreateNetworkMessage();
            netMsg.Type = "FILE_REJECT";
            netMsg.FileId = transferId;
            netMsg.RecipientId = peerId;

            if (_serverConnection != null) await _serverConnection.SendMessageAsync(netMsg);
        }

        private void HandleServerMessage(NetworkMessage msg)
        {
            if (msg.SenderId == MyId)
                return;

            if (msg.Type == "HISTORY")
            {
                if (msg.HistoryMessages != null)
                {
                    bool updated = false;
                    foreach (var hist in msg.HistoryMessages)
                    {
                        if (ProcessHistoricalMessage(hist))
                        {
                            updated = true;
                        }
                    }
                    if (updated)
                    {
                        HistorySynced?.Invoke();
                    }
                }
                return;
            }

            string senderIp = !string.IsNullOrWhiteSpace(msg.SenderIp)
                ? msg.SenderIp
                : DiscoveredPeers.TryGetValue(msg.SenderId, out var knownPeer) && !string.IsNullOrWhiteSpace(knownPeer.IpAddress)
                    ? knownPeer.IpAddress
                    : "127.0.0.1";

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

            if (msg.Type == "HELLO" || msg.Type == "JOIN" || msg.Type == "HEARTBEAT")
            {
                bool isNew = false;
                bool wentOnline = false;
                var now = DateTime.UtcNow;

                if (!DiscoveredPeers.TryGetValue(msg.SenderId, out var p))
                {
                    p = new PeerInfo { Id = msg.SenderId };
                    isNew = DiscoveredPeers.TryAdd(msg.SenderId, p);
                    if (!isNew && !DiscoveredPeers.TryGetValue(msg.SenderId, out p))
                        return;
                }

                lock (p)
                {
                    UpdatePeerFromMessage(p, msg, senderIp, now);
                    if (p.Status == "offline")
                    {
                        wentOnline = true;
                        p.OnlineSince = DateTime.Now;
                    }

                    p.Status = "online";
                }

                if (isNew)
                {
                    PeerOnline?.Invoke(p);
                    NotificationService.PlayOnlineSound();
                    NotificationService.ShowToast($"{p.Username} is Online", "A new peer joined the local network.");
                }
                else if (wentOnline)
                {
                    PeerOnline?.Invoke(p);
                    NotificationService.PlayOnlineSound();
                    NotificationService.ShowToast($"{p.Username} is Online", "Peer returned online.");
                }
                else
                {
                    PeerUpdated?.Invoke(p);
                }
                return;
            }

            if (msg.IsPrivate && !string.Equals(msg.RecipientId, MyId, StringComparison.Ordinal))
                return;

            bool wasKnown = DiscoveredPeers.ContainsKey(msg.SenderId);
            PeerInfo peer = TouchPeerFromTcpMessage(msg, senderIp);

            if (!wasKnown || peer.Status == "offline")
            {
                peer.Status = "online";
                peer.OnlineSince = DateTime.Now;
                PeerOnline?.Invoke(peer);
            }
            else
            {
                PeerUpdated?.Invoke(peer);
            }

            switch (msg.Type)
            {
                case "CHAT":
                    // For incoming file messages, determine download path and register as pending
                    string displayFilePath = string.Empty;
                    if (msg.FileSize > 0 && !string.IsNullOrEmpty(msg.FileId))
                    {
                        string targetFolder = ConfigManager.Config.DownloadFolder;
                        if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
                        {
                            targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                            if (!Directory.Exists(targetFolder))
                                Directory.CreateDirectory(targetFolder);
                        }

                        string targetFilePath = Path.Combine(targetFolder, Path.GetFileName(msg.FileName));
                        if (File.Exists(targetFilePath))
                        {
                            string nameWithoutExt = Path.GetFileNameWithoutExtension(msg.FileName);
                            string ext = Path.GetExtension(msg.FileName);
                            int counter = 1;
                            do
                            {
                                targetFilePath = Path.Combine(targetFolder, $"{nameWithoutExt} ({counter}){ext}");
                                counter++;
                            } while (File.Exists(targetFilePath));
                        }

                        displayFilePath = targetFilePath;

                        // Register as pending download — always resolved via server
                        PendingFileDownloads[msg.FileId] = new PendingFileDownload
                        {
                            FileId = msg.FileId,
                            FileName = msg.FileName,
                            FileSize = msg.FileSize,
                            SenderId = msg.SenderId,
                            SenderUsername = msg.SenderUsername,
                            // Server IP for download (serverIp is always the current connected server)
                            SenderIp = ConfigManager.Config.ServerIp,
                            FilePort = ConfigManager.Config.ServerPort,
                            ReceivedAt = DateTime.UtcNow
                        };
                    }

                    var chatMsg = new ChatMessage
                    {
                        Id = string.IsNullOrEmpty(msg.FileId) ? Guid.NewGuid().ToString("N") : msg.FileId,
                        SenderId = msg.SenderId,
                        SenderUsername = msg.SenderUsername,
                        Text = msg.Content,
                        FileId = msg.FileId,
                        IsPrivate = msg.IsPrivate,
                        RecipientId = msg.RecipientId,
                        FilePath = displayFilePath,
                        FileSize = msg.FileSize,
                        Timestamp = msg.Timestamp
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
                    HandleIncomingFileRequest(msg, senderIp);
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

        private void HandleIncomingFileRequest(NetworkMessage msg, string senderIp)
        {
            // FILE_REQ is a legacy P2P type. In server-buffered mode, files come as CHAT messages.
            // Keep this handler for backward compatibility — just register as pending for manual download.
            string safeFileName = Path.GetFileName(msg.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
                safeFileName = "received_file";

            string targetFolder = ConfigManager.Config.DownloadFolder;
            if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
            {
                targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(targetFolder))
                    Directory.CreateDirectory(targetFolder);
            }

            string targetFilePath = Path.Combine(targetFolder, safeFileName);
            if (File.Exists(targetFilePath))
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
                string ext = Path.GetExtension(safeFileName);
                int counter = 1;
                do
                {
                    targetFilePath = Path.Combine(targetFolder, $"{nameWithoutExt} ({counter}){ext}");
                    counter++;
                } while (File.Exists(targetFilePath));
            }

            // Register as pending - user downloads manually via server-buffered flow
            PendingFileDownloads.TryAdd(msg.FileId, new PendingFileDownload
            {
                FileId = msg.FileId,
                FileName = safeFileName,
                FileSize = msg.FileSize,
                SenderId = msg.SenderId,
                SenderUsername = msg.SenderUsername,
                SenderIp = ConfigManager.Config.ServerIp,
                FilePort = ConfigManager.Config.ServerPort,
                ReceivedAt = DateTime.UtcNow
            });

            var chatMsg = new ChatMessage
            {
                Id = msg.FileId,
                SenderId = msg.SenderId,
                SenderUsername = msg.SenderUsername,
                Text = $"Đã gửi file: {safeFileName}",
                FileId = msg.FileId,
                IsPrivate = true,
                RecipientId = MyId,
                FilePath = targetFilePath,
                FileSize = msg.FileSize,
                Timestamp = DateTime.UtcNow
            };

            HistoryService.AddPrivateMessage(msg.SenderId, chatMsg);
            NotificationService.PlayMessageSound();
            NotificationService.ShowToast($"File từ {msg.SenderUsername}", safeFileName);
            PrivateMessageReceived?.Invoke(msg.SenderId, chatMsg);
        }

        private void EvaluatePeerTimeouts(object? state)
        {
            var now = DateTime.UtcNow;
            // Take a snapshot to avoid modifying collection while iterating
            var peers = DiscoveredPeers.Values.ToArray();
            for (int i = 0; i < peers.Length; i++)
            {
                var peer = peers[i];
                if (peer.Status == "offline")
                    continue;

                if ((now - peer.LastSeen).TotalSeconds >= 15)
                {
                    lock (peer)
                    {
                        peer.Status = "offline";
                        peer.IsTyping = false;
                    }
                    PeerOffline?.Invoke(peer);
                    NotificationService.ShowToast($"{peer.Username} went Offline", "User timed out on local network.");
                }
                else if (peer.IsTyping && (now - peer.TypingStartTime).TotalSeconds >= 4)
                {
                    peer.IsTyping = false;
                    PeerTypingChanged?.Invoke(peer, false);
                }
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

        private bool ProcessHistoricalMessage(NetworkMessage msg)
        {
            if (msg.IsPrivate && !string.Equals(msg.RecipientId, MyId, StringComparison.Ordinal) && !string.Equals(msg.SenderId, MyId, StringComparison.Ordinal))
                return false;

            // Determine the saved/expected local file path for file messages
            string filePath = string.Empty;
            if (!string.IsNullOrEmpty(msg.FileId) && msg.FileSize > 0 && !string.IsNullOrEmpty(msg.FileName))
            {
                string targetFolder = ConfigManager.Config.DownloadFolder;
                if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
                    targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

                filePath = Path.Combine(targetFolder, Path.GetFileName(msg.FileName));

                // If sender is me, point to original source (no pending needed)
                // If sender is someone else, register as pending so user can download from server
                if (!string.Equals(msg.SenderId, MyId, StringComparison.Ordinal))
                {
                    // Only register pending if not already downloaded
                    if (!File.Exists(filePath))
                    {
                        PendingFileDownloads.TryAdd(msg.FileId, new PendingFileDownload
                        {
                            FileId = msg.FileId,
                            FileName = msg.FileName,
                            FileSize = msg.FileSize,
                            SenderId = msg.SenderId,
                            SenderUsername = msg.SenderUsername,
                            SenderIp = ConfigManager.Config.ServerIp,
                            FilePort = ConfigManager.Config.ServerPort,
                            ReceivedAt = msg.Timestamp
                        });
                    }
                }
            }

            var chatMsg = new ChatMessage
            {
                Id = msg.Id,
                SenderId = msg.SenderId,
                SenderUsername = msg.SenderUsername,
                Text = msg.Content,
                FileId = msg.FileId,
                IsPrivate = msg.IsPrivate,
                RecipientId = msg.RecipientId,
                FilePath = filePath,
                FileSize = msg.FileSize,
                Timestamp = msg.Timestamp
            };

            if (msg.IsPrivate)
            {
                string peerId = msg.SenderId == MyId ? msg.RecipientId : msg.SenderId;
                return HistoryService.AddPrivateMessage(peerId, chatMsg);
            }
            else
            {
                return HistoryService.AddGroupMessage(chatMsg);
            }
        }
    }
}
