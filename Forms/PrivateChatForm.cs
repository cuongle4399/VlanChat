using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using LANChatPro.Models;
using LANChatPro.Services;
using LANChatPro.Utils;

namespace LANChatPro.Forms
{
    public partial class PrivateChatForm : Form
    {
        private readonly ChatService _chatService;
        private readonly string _peerId;
        private PeerInfo _peer;
        private bool _isTyping = false;
        private DateTime _lastTypingTime = DateTime.MinValue;
        private readonly System.Windows.Forms.Timer _typingTimer = new();
        private readonly List<FileLinkRange> _fileLinks = new();

        private sealed class FileLinkRange
        {
            public int Start { get; init; }
            public int Length { get; init; }
            public string FileId { get; init; } = string.Empty;

            public bool Contains(int index) => index >= Start && index < Start + Length;
        }

        public PrivateChatForm(ChatService chatService, string peerId)
        {
            InitializeComponent();
            _chatService = chatService;
            _peerId = peerId;

            if (!_chatService.DiscoveredPeers.TryGetValue(peerId, out var peer))
            {
                throw new ArgumentException("Peer not found on LAN", nameof(peerId));
            }
            _peer = peer;

            this.Text = $"Chat with {_peer.Username} ({_peer.IpAddress})";
            lblPeerName.Text = _peer.Username;
            lblPeerName.AutoEllipsis = true;
            lblStatus.AutoEllipsis = true;
            lblFileName.AutoEllipsis = true;
            lblProgressDetails.AutoEllipsis = true;
            lblStatus.Text = _peer.Status == "online" ? "🟢 Online" : "⚫ Offline";
            rtbChat.DetectUrls = false;
            rtbChat.MouseMove += RtbChat_MouseMove;
            rtbChat.MouseDown += RtbChat_MouseDown;

_chatService.PrivateMessageReceived += ChatService_PrivateMessageReceived;
            _chatService.PeerOffline += ChatService_PeerOffline;
            _chatService.PeerOnline += ChatService_PeerOnline;
            _chatService.PeerUpdated += ChatService_PeerUpdated;
            _chatService.PeerTypingChanged += ChatService_PeerTypingChanged;

            _chatService.FileTransferService.TransferUpdated += Transfer_Updated;
            _chatService.FileTransferService.TransferCompleted += Transfer_Completed;
            _chatService.FileTransferService.TransferFailed += Transfer_Failed;

LoadHistory();

_typingTimer.Interval = 1000;
            _typingTimer.Tick += TypingTimer_Tick;
            _typingTimer.Start();
        }

        private void BeginInvokeIfReady(Action action)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_isTyping)
            {
                _isTyping = false;
                _ = _chatService.SendTypingStateAsync(_peerId, false);
            }

_chatService.PrivateMessageReceived -= ChatService_PrivateMessageReceived;
            _chatService.PeerOffline -= ChatService_PeerOffline;
            _chatService.PeerOnline -= ChatService_PeerOnline;
            _chatService.PeerUpdated -= ChatService_PeerUpdated;
            _chatService.PeerTypingChanged -= ChatService_PeerTypingChanged;

            _chatService.FileTransferService.TransferUpdated -= Transfer_Updated;
            _chatService.FileTransferService.TransferCompleted -= Transfer_Completed;
            _chatService.FileTransferService.TransferFailed -= Transfer_Failed;

            _typingTimer.Stop();
            _typingTimer.Dispose();

            base.OnFormClosed(e);
        }

        private void LoadHistory()
        {
            rtbChat.Clear();
            _fileLinks.Clear();
            var messages = _chatService.HistoryService.GetPrivateHistory(_peerId);
            foreach (var msg in messages)
            {
                AppendChatMessage(msg);
            }
        }

        private void AppendChatMessage(ChatMessage msg)
        {
            if (!string.IsNullOrWhiteSpace(msg.FilePath) && msg.FileSize > 0)
            {
                AppendFileMessage(msg, msg.SenderId == _chatService.MyId);
                return;
            }

            AppendChatMessage(msg.SenderUsername, msg.Text, msg.FormattedTimestamp, msg.SenderId == _chatService.MyId);
        }

        private void AppendChatMessage(string sender, string text, string time, bool isMe)
        {
            if (rtbChat.IsDisposed)
                return;

            if (rtbChat.InvokeRequired)
            {
                BeginInvokeIfReady(() => AppendChatMessage(sender, text, time, isMe));
                return;
            }

            int start = rtbChat.TextLength;

rtbChat.SelectionStart = start;
            rtbChat.SelectionColor = Color.FromArgb(148, 155, 164);
            rtbChat.AppendText($"[{time}] ");

rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = isMe ? Color.FromArgb(88, 101, 242) : Color.FromArgb(35, 165, 90);
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
            rtbChat.AppendText($"{sender}: ");

rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.FromArgb(219, 222, 225);
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.AppendText($"{text}{Environment.NewLine}");

            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.ScrollToCaret();
        }

        private void AppendFileMessage(ChatMessage msg, bool isMe)
        {
            if (rtbChat.IsDisposed)
                return;

            if (rtbChat.InvokeRequired)
            {
                BeginInvokeIfReady(() => AppendFileMessage(msg, isMe));
                return;
            }

            string fileName = Path.GetFileName(msg.FilePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "file";
            }

            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.FromArgb(148, 155, 164);
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.AppendText($"[{msg.FormattedTimestamp}] ");

            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = isMe ? Color.FromArgb(88, 101, 242) : Color.FromArgb(35, 165, 90);
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
            rtbChat.AppendText($"{msg.SenderUsername}: ");

            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.FromArgb(219, 222, 225);
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.AppendText($"[File] {fileName} ({Helpers.FormatFileSize(msg.FileSize)}) ");

if (_chatService.FileTransferService.Transfers.TryGetValue(msg.Id, out var transfer))
            {
                if (transfer.Status == FileTransferStatus.Pending)
                {
                    rtbChat.SelectionStart = rtbChat.TextLength;
                    rtbChat.SelectionColor = Color.FromArgb(255, 179, 102);
                    rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Italic);
                    rtbChat.AppendText("Đang chuẩn bị... ⏳");
                }
                else if (transfer.Status == FileTransferStatus.Transferring)
                {
                    int percent = Math.Clamp((int)transfer.ProgressPercentage, 0, 100);
                    rtbChat.SelectionStart = rtbChat.TextLength;
                    rtbChat.SelectionColor = Color.FromArgb(88, 166, 255);
                    rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
                    rtbChat.AppendText(isMe ? $"Đang gửi ({percent}%) 🔄" : $"Đang nhận ({percent}%) 🔄");
                }
                else if (transfer.Status == FileTransferStatus.Completed)
                {
                    AppendCompletedFileStatus(msg, isMe);
                }
                else
                {
                    rtbChat.SelectionStart = rtbChat.TextLength;
                    rtbChat.SelectionColor = Color.FromArgb(231, 76, 60);
                    rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Italic);
                    rtbChat.AppendText("Lỗi truyền tải ❌");
                }
            }
            else
            {

                AppendCompletedFileStatus(msg, isMe);
            }

            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.AppendText(Environment.NewLine);
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.ScrollToCaret();
        }

        private void AppendCompletedFileStatus(ChatMessage msg, bool isMe)
        {
            if (isMe)
            {
                rtbChat.SelectionStart = rtbChat.TextLength;
                rtbChat.SelectionColor = Color.FromArgb(46, 204, 113);
                rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
                rtbChat.AppendText("Đã gửi ✓");
            }
            else
            {
                int linkStart = rtbChat.TextLength;
                string linkText = File.Exists(msg.FilePath) ? "Mở thư mục 📂" : "Tải xuống";
                rtbChat.SelectionStart = linkStart;
                rtbChat.SelectionColor = Color.FromArgb(88, 166, 255);
                rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold | FontStyle.Underline);
                rtbChat.AppendText(linkText);
                _fileLinks.Add(new FileLinkRange
                {
                    Start = linkStart,
                    Length = linkText.Length,
                    FileId = msg.Id
                });
            }
        }

        private void RtbChat_MouseMove(object? sender, MouseEventArgs e)
        {
            rtbChat.Cursor = GetFileLinkAtPoint(e.Location) != null ? Cursors.Hand : Cursors.IBeam;
        }

        private void RtbChat_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            FileLinkRange? link = GetFileLinkAtPoint(e.Location);
            if (link != null)
            {
                HandleFileLinkClicked(link.FileId);
            }
        }

        private FileLinkRange? GetFileLinkAtPoint(Point point)
        {
            if (_fileLinks.Count == 0)
                return null;

            int index = rtbChat.GetCharIndexFromPosition(point);
            return _fileLinks.FirstOrDefault(link => link.Contains(index));
        }

        private void HandleFileLinkClicked(string fileId)
        {
            var messages = _chatService.HistoryService.GetPrivateHistory(_peerId);
            var msg = messages.FirstOrDefault(m => m.Id == fileId);
            if (msg == null)
                return;

            bool isMe = msg.SenderId == _chatService.MyId;

            if (isMe)
            {
                OpenDownloadedFileOrFolder(msg.FilePath);
            }
            else
            {
                if (File.Exists(msg.FilePath))
                {
                    OpenDownloadedFileOrFolder(msg.FilePath);
                }
                else
                {
                    if (_chatService.TryGetPendingFileDownload(fileId, out var pendingFile) && pendingFile != null)
                    {
                        DownloadPendingFile(fileId);
                    }
                    else
                    {
                        MessageBox.Show("File không tồn tại hoặc đã bị xóa.", "Mở file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void OpenDownloadedFileOrFolder(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string argument = $"/select,\"{filePath}\"";
                    System.Diagnostics.Process.Start("explorer.exe", argument);
                }
                else
                {
                    string? directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"\"{directory}\"");
                    }
                    else
                    {
                        MessageBox.Show("Thư mục chứa file không tồn tại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở file/thư mục: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DownloadPendingFile(string fileId)
        {
            if (!_chatService.TryGetPendingFileDownload(fileId, out var pendingFile) || pendingFile == null)
            {
                MessageBox.Show("File này không còn khả dụng để tải. Người gửi có thể đã đóng ứng dụng hoặc phiên gửi đã hết hạn.", "Tải file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using SaveFileDialog sfd = new SaveFileDialog
            {
                FileName = pendingFile.FileName,
                InitialDirectory = _chatService.ConfigManager.Config.DownloadFolder,
                Title = "Lưu file nhận được"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            if (_chatService.StartPendingFileDownload(fileId, sfd.FileName))
            {
                AppendChatMessage("System", $"Đang tải file '{pendingFile.FileName}'...", DateTime.Now.ToString("HH:mm:ss"), false);
            }
            else
            {
                MessageBox.Show("Không thể bắt đầu tải file này.", "Tải file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void btnSend_Click(object? sender, EventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text))
                return;

bool wasTyping = _isTyping;
            _isTyping = false;
            txtMessage.Clear();
            if (wasTyping)
            {
                await _chatService.SendTypingStateAsync(_peerId, false);
            }

            bool ok = await _chatService.SendPrivateMessageAsync(_peerId, text);
            if (ok)
            {
                AppendChatMessage(_chatService.ConfigManager.Config.Username, text, DateTime.Now.ToString("HH:mm:ss"), true);
            }
            else
            {
                AppendChatMessage("System", "Failed to deliver message. The user may have dropped offline.", DateTime.Now.ToString("HH:mm:ss"), false);
            }
        }

private async void txtMessage_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                if (_isTyping)
                {
                    _isTyping = false;
                    await _chatService.SendTypingStateAsync(_peerId, false);
                }

                return;
            }

            if (!_isTyping)
            {
                _isTyping = true;
                _lastTypingTime = DateTime.UtcNow;
                await _chatService.SendTypingStateAsync(_peerId, true);
            }
            else
            {
                _lastTypingTime = DateTime.UtcNow;
            }
        }

        private async void TypingTimer_Tick(object? sender, EventArgs e)
        {
            if (_isTyping && (DateTime.UtcNow - _lastTypingTime).TotalSeconds >= 2)
            {
                _isTyping = false;
                await _chatService.SendTypingStateAsync(_peerId, false);
            }
        }

private async void btnSendFile_Click(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select File to Send";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = ofd.FileName;
                    string? sentTransferId = null;
                    try
                    {

                        await _chatService.FileTransferService.StartSendSessionAsync(
                            filePath,
                            _peerId,
                            _peer.Username,
                            _peer.IpAddress,
                            async (transferId, dynamicPort) =>
                            {
                                sentTransferId = transferId;

                                return await _chatService.SendFileRequestAsync(_peerId, filePath, dynamicPort, transferId);
                            });

                        FileInfo fileInfo = new FileInfo(filePath);
                        if (!string.IsNullOrEmpty(sentTransferId))
                        {
                            AppendFileMessage(new ChatMessage
                            {
                                Id = sentTransferId,
                                SenderId = _chatService.MyId,
                                SenderUsername = _chatService.ConfigManager.Config.Username,
                                Text = $"Đã gửi file: {fileInfo.Name}",
                                IsPrivate = true,
                                RecipientId = _peerId,
                                FilePath = filePath,
                                FileSize = fileInfo.Length,
                                Timestamp = DateTime.UtcNow
                            }, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to initiate file transfer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

private void btnEmoji_Click(object? sender, EventArgs e)
        {
            ContextMenuStrip emojiMenu = new ContextMenuStrip();
            emojiMenu.BackColor = Color.FromArgb(43, 45, 49);
            emojiMenu.ForeColor = Color.White;

            (string Icon, string Label)[] emojis = {
                ("😀", "Cười"),
                ("😂", "Vui"),
                ("❤️", "Tim"),
                ("👍", "Đồng ý"),
                ("🔥", "Hay"),
                ("😎", "Ngầu"),
                ("👋", "Chào"),
                ("🎉", "Chúc mừng"),
                ("😱", "Bất ngờ"),
                ("😢", "Buồn")
            };

            foreach (var emoji in emojis)
            {
                var item = new ToolStripMenuItem($"{emoji.Icon}  {emoji.Label}")
                {
                    ForeColor = Color.White
                };
                item.Click += (s, ev) =>
                {
                    txtMessage.AppendText(emoji.Icon);
                    txtMessage.Focus();
                };
                emojiMenu.Items.Add(item);
            }

            emojiMenu.Show(btnEmoji, new Point(0, -emojiMenu.Height));
        }

private void ChatService_PrivateMessageReceived(string senderId, ChatMessage msg)
        {
            if (senderId == _peerId)
            {
                AppendChatMessage(msg);
            }
        }

        private void ChatService_PeerOffline(PeerInfo peer)
        {
            if (peer.Id == _peerId)
            {
                _peer = peer;
                BeginInvokeIfReady(() => {
                    lblStatus.Text = "⚫ Offline";
                });
            }
        }

        private void ChatService_PeerOnline(PeerInfo peer)
        {
            if (peer.Id == _peerId)
            {
                _peer = peer;
                BeginInvokeIfReady(() => {
                    lblStatus.Text = "🟢 Online";
                });
            }
        }

        private void ChatService_PeerUpdated(PeerInfo peer)
        {
            if (peer.Id == _peerId)
            {
                _peer = peer;
                BeginInvokeIfReady(() => {
                    lblPeerName.Text = peer.Username;
                    this.Text = $"Chat with {peer.Username} ({peer.IpAddress})";
                });
            }
        }

        private void ChatService_PeerTypingChanged(PeerInfo peer, bool isTyping)
        {
            if (peer.Id == _peerId)
            {
                BeginInvokeIfReady(() => {
                    if (isTyping)
                    {
                        lblStatus.Text = "🟡 Typing...";
                    }
                    else
                    {
                        lblStatus.Text = _peer.Status == "online" ? "🟢 Online" : "⚫ Offline";
                    }
                });
            }
        }

private void Transfer_Updated(FileTransferInfo transfer)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (transfer.PeerId != _peerId) return;

            BeginInvokeIfReady(() =>
            {
                pnlFileProgress.Visible = true;
                lblFileName.Text = $"{transfer.Direction}: {transfer.FileName}";

                int percent = Math.Clamp((int)transfer.ProgressPercentage, 0, 100);
                prgFile.Value = percent;

                lblProgressDetails.Text = $"{Helpers.FormatFileSize(transfer.BytesTransferred)} / {Helpers.FormatFileSize(transfer.FileSize)} ({percent}%) @ {transfer.SpeedString}";
                lblTimeElapsed.Text = $"Time: {transfer.ElapsedTimeString}";

LoadHistory();
            });
        }

        private void Transfer_Completed(FileTransferInfo transfer)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (transfer.PeerId != _peerId) return;

            BeginInvokeIfReady(() =>
            {
                prgFile.Value = 100;
                lblProgressDetails.Text = "Completed successfully!";

LoadHistory();

Task.Delay(3000).ContinueWith(_ =>
                {
                    try
                    {
                        if (this.IsDisposed || this.Disposing) return;
                        BeginInvokeIfReady(() => pnlFileProgress.Visible = false);
                    }
                    catch { }
                });
            });
        }

        private void Transfer_Failed(FileTransferInfo transfer, string reason)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (transfer.PeerId != _peerId) return;

            BeginInvokeIfReady(() =>
            {
                prgFile.Value = 0;
                lblProgressDetails.Text = $"Failed: {reason}";

LoadHistory();

                Task.Delay(5000).ContinueWith(_ =>
                {
                    try
                    {
                        if (this.IsDisposed || this.Disposing) return;
                        BeginInvokeIfReady(() => pnlFileProgress.Visible = false);
                    }
                    catch { }
                });
            });
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                btnSend_Click(this, EventArgs.Empty);
            }
        }
    }
}
