using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LANChatServer.Models;

namespace LANChatServer
{
    public partial class ServerForm : Form
    {
        private readonly ConcurrentDictionary<string, ClientSession> _clients = new();
        private TcpListener? _listener;
        private TcpListener? _fileListener;
        private CancellationTokenSource? _cts;
        private bool _isRunning = false;

        public ServerForm()
        {
            InitializeComponent();
            
            // Register close event handler
            this.FormClosing += ServerForm_FormClosing;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Set UI details
            lblStatusDot.Region = new Region(new Rectangle(0, 0, lblStatusDot.Width, lblStatusDot.Height));
            DrawRoundedDot(lblStatusDot, Color.FromArgb(242, 63, 67));

            // Load local IPs
            LoadLocalIps();

            // Initialize DB
            DbService.OnLog += (msg, color) => Log(msg, color);
            DbService.Initialize();

            // Start server automatically on startup
            AutoStartServer();
        }

        private async void AutoStartServer()
        {
            int port = (int)nudPort.Value;
            bool success = await StartServerAsync(port);
            if (success)
            {
                _isRunning = true;
                btnToggleServer.Text = "DỪNG MÁY CHỦ";
                btnToggleServer.BackColor = Color.FromArgb(242, 63, 67); // Red
                nudPort.Enabled = false;
                lblStatus.Text = $"Đang chạy: Cổng {port}";
                lblStatus.ForeColor = Color.FromArgb(35, 165, 90); // Green
                DrawRoundedDot(lblStatusDot, Color.FromArgb(35, 165, 90));
            }
        }

        private static void DrawRoundedDot(Label label, Color color)
        {
            // Remove previous Paint handlers to avoid stacking on each call (memory leak)
            label.Tag = color;
            label.Paint -= OnDotPaint;
            label.Paint += OnDotPaint;
            label.BackColor = Color.Transparent;
            label.Invalidate();
        }

        private static void OnDotPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not Label lbl || lbl.Tag is not Color color) return;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(color);
            e.Graphics.FillEllipse(brush, 0, 0, lbl.Width - 1, lbl.Height - 1);
        }

        private void LoadLocalIps()
        {
            lstIps.Items.Clear();
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                bool found = false;
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (ip.ToString() != "127.0.0.1")
                        {
                            lstIps.Items.Add(ip.ToString());
                            found = true;
                        }
                    }
                }
                if (!found)
                {
                    lstIps.Items.Add("127.0.0.1");
                }
            }
            catch (Exception ex)
            {
                Log($"[WARN] Không thể lấy địa chỉ IP mạng: {ex.Message}", Color.FromArgb(240, 167, 4));
                lstIps.Items.Add("127.0.0.1");
            }
        }

        private async void btnToggleServer_Click(object? sender, EventArgs e)
        {
            if (!_isRunning)
            {
                int port = (int)nudPort.Value;
                btnToggleServer.Enabled = false;
                bool success = await StartServerAsync(port);
                btnToggleServer.Enabled = true;

                if (success)
                {
                    _isRunning = true;
                    btnToggleServer.Text = "DỪNG MÁY CHỦ";
                    btnToggleServer.BackColor = Color.FromArgb(242, 63, 67); // Red
                    nudPort.Enabled = false;
                    lblStatus.Text = $"Đang chạy: Cổng {port}";
                    lblStatus.ForeColor = Color.FromArgb(35, 165, 90); // Green
                    DrawRoundedDot(lblStatusDot, Color.FromArgb(35, 165, 90));
                }
            }
            else
            {
                btnToggleServer.Enabled = false;
                StopServer();
                btnToggleServer.Enabled = true;

                _isRunning = false;
                btnToggleServer.Text = "KHỞI ĐỘNG SERVER";
                btnToggleServer.BackColor = Color.FromArgb(88, 101, 242); // Blurple
                nudPort.Enabled = true;
                lblStatus.Text = "Trạng thái: Đã dừng";
                lblStatus.ForeColor = Color.FromArgb(242, 63, 67); // Red
                DrawRoundedDot(lblStatusDot, Color.FromArgb(242, 63, 67));
            }
        }

        private async Task<bool> StartServerAsync(int port)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();

                int filePort = port + 1;
                _fileListener = new TcpListener(IPAddress.Any, filePort);
                _fileListener.Start();

                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                _ = Task.Run(() => ListenLoopAsync(_listener, token), token);
                _ = Task.Run(() => FileServerListenLoopAsync(_fileListener, token), token);

                Log($"[SERVER] Server listening on port {port}...", Color.FromArgb(35, 165, 90));
                Log($"[SERVER] File server listening on port {filePort}...", Color.FromArgb(35, 165, 90));
                Log("[SERVER] Đang đợi kết nối mạng VLAN/LAN...", Color.FromArgb(148, 155, 164));
                return true;
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Không thể khởi động Server: {ex.Message}", Color.FromArgb(242, 63, 67));
                MessageBox.Show($"Lỗi khởi động Server: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void StopServer()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _fileListener?.Stop();

                // Disconnect and dispose all sessions
                foreach (var session in _clients.Values)
                {
                    try { session.Dispose(); }
                    catch { }
                }
                _clients.Clear();

                Log("[SERVER] Máy chủ đã dừng hoạt động.", Color.FromArgb(242, 63, 67));
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Lỗi khi dừng Server: {ex.Message}", Color.FromArgb(242, 63, 67));
            }
        }

        private async Task FileServerListenLoopAsync(TcpListener listener, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(ct);
                    _ = Task.Run(() => HandleFileClientAsync(client, ct), ct);
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private async Task HandleFileClientAsync(TcpClient client, CancellationToken ct)
        {
            string clientIp = client.Client.RemoteEndPoint is IPEndPoint endPoint
                ? endPoint.Address.ToString()
                : "127.0.0.1";

            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    string? header = await ReadLineAsync(stream, ct);
                    if (string.IsNullOrEmpty(header))
                        return;

                    string[] parts = header.Split('|');
                    if (parts.Length == 0)
                        return;

                    string command = parts[0];
                    if (command == "UPLOAD")
                    {
                        if (parts.Length < 4)
                        {
                            byte[] err = Encoding.UTF8.GetBytes("ERROR|Invalid header format\n");
                            await stream.WriteAsync(err, 0, err.Length, ct);
                            return;
                        }

                        string fileId = parts[1];
                        string fileName = parts[2];
                        if (!long.TryParse(parts[3], out long fileSize))
                        {
                            byte[] err = Encoding.UTF8.GetBytes("ERROR|Invalid file size\n");
                            await stream.WriteAsync(err, 0, err.Length, ct);
                            return;
                        }

                        string uploadsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");
                        if (!Directory.Exists(uploadsDir))
                        {
                            Directory.CreateDirectory(uploadsDir);
                        }

                        string filePath = Path.Combine(uploadsDir, fileId);
                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[65536];
                            long totalRead = 0;
                            while (totalRead < fileSize)
                            {
                                int toRead = (int)Math.Min(buffer.Length, fileSize - totalRead);
                                int read = await stream.ReadAsync(buffer, 0, toRead, ct);
                                if (read == 0)
                                    throw new IOException("Client disconnected during upload.");

                                await fileStream.WriteAsync(buffer, 0, read, ct);
                                totalRead += read;
                            }
                        }

                        byte[] ok = Encoding.UTF8.GetBytes("OK\n");
                        await stream.WriteAsync(ok, 0, ok.Length, ct);
                        Log($"[FILE] '{clientIp}' đã tải lên thành công: {fileName} (ID: {fileId}, {fileSize} bytes)", Color.FromArgb(35, 165, 90));
                    }
                    else if (command == "DOWNLOAD")
                    {
                        if (parts.Length < 2)
                        {
                            byte[] err = Encoding.UTF8.GetBytes("ERROR|Invalid header format\n");
                            await stream.WriteAsync(err, 0, err.Length, ct);
                            return;
                        }

                        string fileId = parts[1];
                        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads", fileId);

                        if (!File.Exists(filePath))
                        {
                            byte[] err = Encoding.UTF8.GetBytes("ERROR|File not found\n");
                            await stream.WriteAsync(err, 0, err.Length, ct);
                            return;
                        }

                        var fileInfo = new FileInfo(filePath);
                        long fileSize = fileInfo.Length;

                        byte[] ok = Encoding.UTF8.GetBytes($"OK|{fileSize}\n");
                        await stream.WriteAsync(ok, 0, ok.Length, ct);

                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            byte[] buffer = new byte[65536];
                            int read;
                            while ((read = await fileStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                            {
                                await stream.WriteAsync(buffer, 0, read, ct);
                            }
                        }
                        Log($"[FILE] '{clientIp}' đã tải xuống thành công file ID: {fileId}", Color.FromArgb(35, 165, 90));
                    }
                }
                catch (Exception ex)
                {
                    Log($"[FILE ERROR] Lỗi truyền tải file từ '{clientIp}': {ex.Message}", Color.FromArgb(242, 63, 67));
                }
            }
        }

        private async Task ListenLoopAsync(TcpListener listener, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(ct);
                    _ = Task.Run(() => HandleClientAsync(client, ct), ct);
                }
                catch (Exception)
                {
                    // Likely listener stopped
                    break;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            string clientIp = client.Client.RemoteEndPoint is IPEndPoint endPoint
                ? endPoint.Address.ToString()
                : IPAddress.Loopback.ToString();

            Log($"[CONNECTION] Kết nối TCP mới từ: {clientIp}", Color.FromArgb(240, 167, 4));

            ClientSession? session = null;
            NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    string? json = await reader.ReadLineAsync(ct);
                    if (string.IsNullOrEmpty(json))
                        break;

                    var msg = JsonSerializer.Deserialize(json, JsonContext.Default.NetworkMessage);
                    if (msg == null) continue;

                    if (session == null)
                    {
                        if (msg.Type == "HELLO" || msg.Type == "JOIN")
                        {
                            session = new ClientSession
                            {
                                Id = msg.SenderId,
                                Username = msg.SenderUsername,
                                MachineName = msg.SenderMachineName,
                                IpAddress = clientIp,
                                Port = msg.SenderPort,
                                AvatarIndex = msg.SenderAvatarIndex,
                                Client = client,
                                Writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true }
                            };

                            _clients[msg.SenderId] = session;
                            Log($"[JOIN] Người dùng '{msg.SenderUsername}' ({msg.SenderId}) đã tham gia từ {clientIp}", Color.FromArgb(35, 165, 90));

                            // Broadcast this HELLO to all other clients
                            msg.Type = "HELLO";
                            msg.SenderIp = clientIp;
                            await BroadcastAsync(msg, excludeId: session.Id);

                            // Send existing users' HELLO back to this new client
                            foreach (var peer in _clients.Values)
                            {
                                if (peer.Id != session.Id)
                                {
                                    var peerHello = new NetworkMessage
                                    {
                                        Type = "HELLO",
                                        SenderId = peer.Id,
                                        SenderUsername = peer.Username,
                                        SenderMachineName = peer.MachineName,
                                        SenderIp = peer.IpAddress,
                                        SenderPort = peer.Port,
                                        SenderAvatarIndex = peer.AvatarIndex
                                    };
                                    await SendToClientAsync(session, peerHello);
                                }
                            }

                            // Fetch and send chat history (Group + Private) to the joined client
                            var groupHistory = DbService.GetGroupHistory(100);
                            var privateHistory = DbService.GetPrivateHistory(session.Id);

                            var combinedHistory = new List<NetworkMessage>();
                            combinedHistory.AddRange(groupHistory);
                            combinedHistory.AddRange(privateHistory);
                            combinedHistory.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                            if (combinedHistory.Count > 0)
                            {
                                var historyMsg = new NetworkMessage
                                {
                                    Type = "HISTORY",
                                    HistoryMessages = combinedHistory
                                };
                                await SendToClientAsync(session, historyMsg);
                                Log($"[HISTORY] Đã gửi {combinedHistory.Count} lịch sử chat cho '{session.Username}' ({session.Id})", Color.FromArgb(0, 176, 244));
                            }
                        }
                        else
                        {
                            Log($"[WARN] Tin nhắn đầu tiên từ {clientIp} không phải là HELLO ({msg.Type})", Color.FromArgb(242, 63, 67));
                            break;
                        }
                    }
                    else
                    {
                        if (msg.Type == "GOODBYE")
                        {
                            break;
                        }

                        StampSenderMetadata(msg, session, clientIp);

                        // Save messages to database
                        if (msg.Type == "CHAT" || msg.Type == "FILE_REQ")
                        {
                            DbService.SaveMessage(msg);
                            if (msg.Type == "CHAT")
                            {
                                if (msg.IsPrivate)
                                {
                                    Log($"[PRIVATE] {msg.SenderUsername} -> {msg.RecipientId}: {msg.Content}", Color.FromArgb(181, 186, 193));
                                }
                                else
                                {
                                    Log($"[PUBLIC] {msg.SenderUsername}: {msg.Content}", Color.FromArgb(227, 229, 233));
                                }
                            }
                        }

                        // If private, route
                        if (msg.IsPrivate && !string.IsNullOrEmpty(msg.RecipientId))
                        {
                            if (_clients.TryGetValue(msg.RecipientId, out var targetSession))
                            {
                                await SendToClientAsync(targetSession, msg);
                            }
                        }
                        else
                        {
                            // Broadcast
                            await BroadcastAsync(msg, excludeId: session.Id);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Connection lost or closed
            }
            finally
            {
                if (session != null)
                {
                    _clients.TryRemove(session.Id, out _);
                    Log($"[LEAVE] Người dùng '{session.Username}' ({session.Id}) đã ngắt kết nối.", Color.FromArgb(240, 167, 4));

                    var goodbyeMsg = new NetworkMessage
                    {
                        Type = "GOODBYE",
                        SenderId = session.Id,
                        SenderUsername = session.Username
                    };
                    await BroadcastAsync(goodbyeMsg, excludeId: session.Id);
                    session.Dispose();
                }
                else
                {
                    client.Close();
                }
            }
        }

        private void StampSenderMetadata(NetworkMessage msg, ClientSession session, string clientIp)
        {
            session.Username = string.IsNullOrWhiteSpace(msg.SenderUsername) ? session.Username : msg.SenderUsername;
            session.MachineName = string.IsNullOrWhiteSpace(msg.SenderMachineName) ? session.MachineName : msg.SenderMachineName;
            session.IpAddress = clientIp;
            if (msg.SenderPort > 0)
            {
                session.Port = msg.SenderPort;
            }
            session.AvatarIndex = msg.SenderAvatarIndex;

            msg.SenderIp = clientIp;
            if (msg.SenderPort <= 0)
            {
                msg.SenderPort = session.Port;
            }
            msg.SenderAvatarIndex = session.AvatarIndex;
        }

        private async Task BroadcastAsync(NetworkMessage msg, string? excludeId = null)
        {
            var tasks = new List<Task>();
            foreach (var session in _clients.Values)
            {
                if (session.Id != excludeId)
                {
                    tasks.Add(SendToClientAsync(session, msg));
                }
            }
            await Task.WhenAll(tasks);
        }

        private async Task SendToClientAsync(ClientSession session, NetworkMessage msg)
        {
            try
            {
                string json = JsonSerializer.Serialize(msg, JsonContext.Default.NetworkMessage);
                await session.WriteLock.WaitAsync();
                try
                {
                    await session.Writer.WriteLineAsync(json);
                }
                finally
                {
                    session.WriteLock.Release();
                }
            }
            catch
            {
                // Writer failed, handling is in client loop
            }
        }

        private void btnOpenFolder_Click(object? sender, EventArgs e)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                if (Directory.Exists(path))
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Không thể mở thư mục: {ex.Message}", Color.FromArgb(242, 63, 67));
                MessageBox.Show($"Lỗi mở thư mục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnResetDb_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn XÓA TOÀN BỘ lịch sử trò chuyện trong cơ sở dữ liệu?\nHành động này không thể hoàn tác!",
                "Xác nhận xóa dữ liệu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    DbService.ResetData();
                    Log("[DB] ĐÃ XÓA TOÀN BỘ LỊCH SỬ TRÒ CHUYỆN TRONG DATABASE.", Color.FromArgb(242, 63, 67));
                    MessageBox.Show("Đã xóa toàn bộ lịch sử trò chuyện thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] Không thể xóa dữ liệu: {ex.Message}", Color.FromArgb(242, 63, 67));
                    MessageBox.Show($"Xóa dữ liệu thất bại: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ServerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            StopServer();
        }

        public void Log(string message, Color color)
        {
            if (rtxtLog.InvokeRequired)
            {
                rtxtLog.BeginInvoke(new Action<string, Color>(Log), message, color);
                return;
            }

            rtxtLog.SelectionStart = rtxtLog.TextLength;
            rtxtLog.SelectionLength = 0;
            rtxtLog.SelectionColor = Color.FromArgb(148, 155, 164); // Dark gray timestamp
            rtxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] ");

            rtxtLog.SelectionColor = color;
            rtxtLog.AppendText(message + Environment.NewLine);
            rtxtLog.SelectionColor = rtxtLog.ForeColor;
            rtxtLog.ScrollToCaret();
        }

        private static async Task<string?> ReadLineAsync(NetworkStream stream, CancellationToken ct)
        {
            var bytes = new System.Collections.Generic.List<byte>();
            byte[] buf = new byte[1];
            while (true)
            {
                int read = await stream.ReadAsync(buf, 0, 1, ct);
                if (read == 0)
                {
                    if (bytes.Count == 0) return null;
                    break;
                }
                byte b = buf[0];
                if (b == '\n') break;
                if (b != '\r') bytes.Add(b);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }

    public class ClientSession : IDisposable
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public int AvatarIndex { get; set; }
        public TcpClient Client { get; set; } = null!;
        public StreamWriter Writer { get; set; } = null!;
        // Prevents concurrent writes from corrupting the stream
        public SemaphoreSlim WriteLock { get; } = new SemaphoreSlim(1, 1);

        public void Dispose()
        {
            WriteLock.Dispose();
            try { Writer?.Dispose(); } catch { }
            try { Client?.Dispose(); } catch { }
        }
    }
}
