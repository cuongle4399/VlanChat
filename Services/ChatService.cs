using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public string MyId => $"{LocalIp}:{ConfigManager.Config.Port}";

        public ConcurrentDictionary<string, PeerInfo> DiscoveredPeers { get; } = new();
        public ConcurrentDictionary<string, PendingFileDownload> PendingFileDownloads { get; } = new();

public event Action<PeerInfo>? PeerOnline;
        public event Action<PeerInfo>? PeerOffline;
        public event Action<PeerInfo>? PeerUpdated;
        public event Action<ChatMessage>? GroupMessageReceived;
        public event Action<string, ChatMessage>? PrivateMessageReceived;
        public event Action<PeerInfo, bool>? PeerTypingChanged;

        private UdpDiscoveryService? _udpDiscovery;
        private TcpServerService? _tcpServer;
        private System.Threading.Timer? _peerTimeoutTimer;
        private System.Threading.Timer? _localPeerRegistryTimer;
        private static readonly string LocalPeerRegistryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LANChatPro",
            "LocalPeers"
        );

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

        public void Start()
        {
            if (_tcpServer != null || _udpDiscovery != null)
                return;

            BindTcpServer();

_udpDiscovery = new UdpDiscoveryService(LocalIp, ConfigManager.Config.Port, CreateNetworkMessage);
            _udpDiscovery.MessageReceived += HandleUdpMessage;
            _udpDiscovery.Start();

_peerTimeoutTimer = new System.Threading.Timer(EvaluatePeerTimeouts, null, 3000, 3000);
            _localPeerRegistryTimer = new System.Threading.Timer(UpdateLocalPeerRegistry, null, 0, 2000);

            Logger.Info($"ChatService running at local IP: {LocalIp}, TCP Port: {ConfigManager.Config.Port}. Unique Node ID: {MyId}");
        }

        private void BindTcpServer()
        {
            int port = ConfigManager.Config.Port;
            while (port <= 65535)
            {
                var tcpServer = new TcpServerService(port);
                tcpServer.MessageReceived += HandleTcpMessage;

                try
                {
                    tcpServer.Start();
                    _tcpServer = tcpServer;
                    ConfigManager.Config.Port = port;
                    return;
                }
                catch (SocketException)
                {
                    tcpServer.MessageReceived -= HandleTcpMessage;
                    tcpServer.Stop();
                    port++;
                }
            }

            throw new InvalidOperationException("No available TCP port found for LAN Chat Pro.");
        }

        private void UpdateLocalPeerRegistry(object? state)
        {
            try
            {
                Directory.CreateDirectory(LocalPeerRegistryPath);

                var self = new PeerInfo
                {
                    Id = MyId,
                    IpAddress = "127.0.0.1",
                    Port = ConfigManager.Config.Port,
                    Username = ConfigManager.Config.Username,
                    MachineName = Environment.MachineName,
                    AvatarIndex = ConfigManager.Config.AvatarIndex,
                    Status = "online",
                    LastSeen = DateTime.UtcNow
                };

                JsonStorage.Save(GetLocalPeerFilePath(MyId), self, JsonContext.Default.PeerInfo);

                foreach (string filePath in Directory.EnumerateFiles(LocalPeerRegistryPath, "*.peer.json"))
                {
                    string peerId = DecodePeerIdFromFileName(filePath);
                    if (string.IsNullOrEmpty(peerId) || peerId == MyId)
                        continue;

                    DateTime lastWriteUtc = File.GetLastWriteTimeUtc(filePath);
                    if ((DateTime.UtcNow - lastWriteUtc).TotalSeconds > 8)
                    {
                        MarkPeerOffline(peerId, "Local test instance stopped.");
                        TryDeleteStaleLocalPeerFile(filePath, lastWriteUtc);
                        continue;
                    }

                    PeerInfo? peerInfo = JsonStorage.Load(filePath, JsonContext.Default.PeerInfo);
                    if (peerInfo == null || string.IsNullOrWhiteSpace(peerInfo.Id) || peerInfo.Id == MyId)
                        continue;

                    RegisterOrUpdatePeer(peerInfo);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Local peer registry update failed", ex);
            }
        }

        private void RegisterOrUpdatePeer(PeerInfo peerInfo)
        {
            bool isNew = false;
            bool wentOnline = false;

            var peer = DiscoveredPeers.GetOrAdd(peerInfo.Id, _ =>
            {
                isNew = true;
                return new PeerInfo { Id = peerInfo.Id };
            });

            lock (peer)
            {
                peer.LastSeen = DateTime.UtcNow;
                peer.Username = peerInfo.Username;
                peer.MachineName = peerInfo.MachineName;
                peer.IpAddress = peerInfo.IpAddress;
                peer.Port = peerInfo.Port;
                peer.AvatarIndex = peerInfo.AvatarIndex;

                if (peer.Status == "offline")
                {
                    wentOnline = true;
                }

                peer.Status = "online";
            }

            if (isNew || wentOnline)
            {
                PeerOnline?.Invoke(peer);
            }
            else
            {
                PeerUpdated?.Invoke(peer);
            }
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

        private void RemoveLocalPeerRegistration()
        {
            try
            {
                string filePath = GetLocalPeerFilePath(MyId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Unable to remove local peer registration: {ex.Message}");
            }
        }

        private static void TryDeleteStaleLocalPeerFile(string filePath, DateTime lastWriteUtc)
        {
            try
            {
                if ((DateTime.UtcNow - lastWriteUtc).TotalSeconds > 30)
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }
        }

        private static string GetLocalPeerFilePath(string peerId)
        {
            return Path.Combine(LocalPeerRegistryPath, $"{EncodePeerId(peerId)}.peer.json");
        }

        private static string EncodePeerId(string peerId)
        {
            return Convert.ToHexString(Encoding.UTF8.GetBytes(peerId));
        }

        private static string DecodePeerIdFromFileName(string filePath)
        {
            string name = Path.GetFileName(filePath);
            const string suffix = ".peer.json";
            if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            string hex = name[..^suffix.Length];
            try
            {
                return Encoding.UTF8.GetString(Convert.FromHexString(hex));
            }
            catch
            {
                return string.Empty;
            }
        }

        public void Stop()
        {
            _peerTimeoutTimer?.Dispose();
            _peerTimeoutTimer = null;

            _localPeerRegistryTimer?.Dispose();
            _localPeerRegistryTimer = null;
            RemoveLocalPeerRegistration();

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
            netMsg.Type = "CHAT";
            netMsg.Content = text;
            netMsg.IsPrivate = false;

var tasks = DiscoveredPeers.Values
                .Where(p => p.Status != "offline")
                .Select(peer => TcpClientService.SendMessageAsync(GetPeerTransportIp(peer), peer.Port, netMsg))
                .ToArray();

            bool[] results = await Task.WhenAll(tasks);
            return results.Length == 0 || results.All(result => result);
        }

        public async Task<bool> SendGroupFileRequestAsync(string filePath, int dynamicFilePort, string transferId)
        {
            var fileInfo = new FileInfo(filePath);

var chatMsg = new ChatMessage
            {
                Id = transferId,
                SenderId = MyId,
                SenderUsername = ConfigManager.Config.Username,
                Text = $"Đã gửi file: {fileInfo.Name}",
                IsPrivate = false,
                FilePath = filePath,
                FileSize = fileInfo.Length,
                Timestamp = DateTime.UtcNow
            };
            HistoryService.AddGroupMessage(chatMsg);

var netMsg = CreateNetworkMessage();
            netMsg.Type = "CHAT";
            netMsg.Content = $"Đã chia sẻ file: {fileInfo.Name}";
            netMsg.IsPrivate = false;
            netMsg.FileId = transferId;
            netMsg.FileName = fileInfo.Name;
            netMsg.FileSize = fileInfo.Length;
            netMsg.FilePort = dynamicFilePort;

var tasks = DiscoveredPeers.Values
                .Where(p => p.Status != "offline")
                .Select(peer => TcpClientService.SendMessageAsync(GetPeerTransportIp(peer), peer.Port, netMsg))
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

            return await TcpClientService.SendMessageAsync(GetPeerTransportIp(peer), peer.Port, netMsg);
        }

        public async Task SendTypingStateAsync(string? peerId, bool isTyping)
        {
            var netMsg = CreateNetworkMessage();
            netMsg.Type = "TYPING";
            netMsg.Content = isTyping.ToString();

            if (string.IsNullOrEmpty(peerId))
            {

                var tasks = DiscoveredPeers.Values
                    .Where(p => p.Status != "offline")
                    .Select(peer => TcpClientService.SendMessageAsync(GetPeerTransportIp(peer), peer.Port, netMsg));
                await Task.WhenAll(tasks);
            }
            else
            {

                if (DiscoveredPeers.TryGetValue(peerId, out var peer))
                {
                    await TcpClientService.SendMessageAsync(GetPeerTransportIp(peer), peer.Port, netMsg);
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
            netMsg.FilePort = dynamicFilePort;

            bool sent = await TcpClientService.SendMessageAsync(GetPeerTransportIp(peer), peer.Port, netMsg);
            if (sent)
            {
                HistoryService.AddPrivateMessage(peerId, new ChatMessage
                {
                    Id = transferId,
                    SenderId = MyId,
                    SenderUsername = ConfigManager.Config.Username,
                    Text = $"Đã gửi file: {fileInfo.Name}",
                    IsPrivate = true,
                    RecipientId = peerId,
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    Timestamp = DateTime.UtcNow
                });
            }

            return sent;
        }

        public bool TryGetPendingFileDownload(string fileId, out PendingFileDownload? pendingFile)
        {
            return PendingFileDownloads.TryGetValue(fileId, out pendingFile);
        }

        public bool StartPendingFileDownload(string fileId, string savePath)
        {
            if (!PendingFileDownloads.TryRemove(fileId, out var pendingFile))
                return false;

            FileTransferService.StartReceiveSession(
                pendingFile.FileId,
                pendingFile.FileName,
                pendingFile.FileSize,
                pendingFile.SenderId,
                pendingFile.SenderUsername,
                pendingFile.SenderIp,
                pendingFile.FilePort,
                savePath);

            return true;
        }

        public async Task SendFileRejectAsync(string peerIp, int peerPort, string transferId)
        {
            var netMsg = CreateNetworkMessage();
            netMsg.Type = "FILE_REJECT";
            netMsg.FileId = transferId;

            await TcpClientService.SendMessageAsync(peerIp, peerPort, netMsg);
        }

private void HandleUdpMessage(NetworkMessage msg, string senderIp)
        {
            if (msg.SenderId == MyId)
                return;

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
                    peer.OnlineSince = DateTime.Now;
                }

                peer.Status = "online";
            }

            if (isNew)
            {
                PeerOnline?.Invoke(peer);
                NotificationService.PlayOnlineSound();
                NotificationService.ShowToast($"{peer.Username} is Online", "A new peer joined the local network.");

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
                    string displayFilePath = msg.FileName;
                    if (msg.FileSize > 0 && !string.IsNullOrEmpty(msg.FileId))
                    {

                        string targetFolder = ConfigManager.Config.DownloadFolder;
                        if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
                        {
                            targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                            if (!Directory.Exists(targetFolder))
                            {
                                Directory.CreateDirectory(targetFolder);
                            }
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

                        string senderResolvedIp = string.Equals(msg.SenderMachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                            ? "127.0.0.1"
                            : senderIp;

                        PendingFileDownloads[msg.FileId] = new PendingFileDownload
                        {
                            FileId = msg.FileId,
                            FileName = msg.FileName,
                            FileSize = msg.FileSize,
                            SenderId = msg.SenderId,
                            SenderUsername = msg.SenderUsername,
                            SenderIp = senderResolvedIp,
                            FilePort = msg.FilePort > 0 ? msg.FilePort : msg.SenderPort,
                            ReceivedAt = DateTime.UtcNow
                        };
                    }

                    var chatMsg = new ChatMessage
                    {
                        Id = string.IsNullOrEmpty(msg.FileId) ? Guid.NewGuid().ToString("N") : msg.FileId,
                        SenderId = msg.SenderId,
                        SenderUsername = msg.SenderUsername,
                        Text = msg.Content,
                        IsPrivate = msg.IsPrivate,
                        RecipientId = msg.RecipientId,
                        FilePath = displayFilePath,
                        FileSize = msg.FileSize,
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
            int filePort = msg.FilePort > 0 ? msg.FilePort : msg.SenderPort;
            string safeFileName = Path.GetFileName(msg.FileName);
            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                safeFileName = "received_file";
            }

string targetFolder = ConfigManager.Config.DownloadFolder;
            if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
            {
                targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }
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

            string senderResolvedIp = string.Equals(msg.SenderMachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                ? "127.0.0.1"
                : senderIp;

            PendingFileDownloads[msg.FileId] = new PendingFileDownload
            {
                FileId = msg.FileId,
                FileName = safeFileName,
                FileSize = msg.FileSize,
                SenderId = msg.SenderId,
                SenderUsername = msg.SenderUsername,
                SenderIp = senderResolvedIp,
                FilePort = filePort,
                ReceivedAt = DateTime.UtcNow
            };

            var chatMsg = new ChatMessage
            {
                Id = msg.FileId,
                SenderId = msg.SenderId,
                SenderUsername = msg.SenderUsername,
                Text = $"Đã gửi file: {safeFileName}",
                IsPrivate = true,
                RecipientId = MyId,
                FilePath = targetFilePath,
                FileSize = msg.FileSize,
                Timestamp = DateTime.UtcNow
            };

            HistoryService.AddPrivateMessage(msg.SenderId, chatMsg);

StartPendingFileDownload(msg.FileId, targetFilePath);

            NotificationService.PlayMessageSound();
            NotificationService.ShowToast($"File từ {msg.SenderUsername}", safeFileName);
            PrivateMessageReceived?.Invoke(msg.SenderId, chatMsg);
        }

        private void EvaluatePeerTimeouts(object? state)
        {
            var now = DateTime.UtcNow;
            foreach (var peer in DiscoveredPeers.Values)
            {
                if (peer.Status != "offline")
                {

                    if ((now - peer.LastSeen).TotalSeconds >= 15)
                    {
                        peer.Status = "offline";
                        peer.IsTyping = false;
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
        }
    }
}
