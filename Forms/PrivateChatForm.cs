using System;
using System.Drawing;
using System.IO;
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

            // Bind events for socket state notifications
            _chatService.PrivateMessageReceived += ChatService_PrivateMessageReceived;
            _chatService.PeerOffline += ChatService_PeerOffline;
            _chatService.PeerOnline += ChatService_PeerOnline;
            _chatService.PeerUpdated += ChatService_PeerUpdated;
            _chatService.PeerTypingChanged += ChatService_PeerTypingChanged;

            _chatService.FileTransferService.TransferUpdated += Transfer_Updated;
            _chatService.FileTransferService.TransferCompleted += Transfer_Completed;
            _chatService.FileTransferService.TransferFailed += Transfer_Failed;

            // Load local history
            LoadHistory();

            // Typing checker timer
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

            // Unsubscribe all events to prevent memory leaks
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
            var messages = _chatService.HistoryService.GetPrivateHistory(_peerId);
            foreach (var msg in messages)
            {
                AppendChatMessage(msg.SenderUsername, msg.Text, msg.FormattedTimestamp, msg.SenderId == _chatService.MyId);
            }
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

            // Colorize timestamp
            rtbChat.SelectionStart = start;
            rtbChat.SelectionColor = Color.FromArgb(148, 155, 164); // Muted timestamp color
            rtbChat.AppendText($"[{time}] ");

            // Colorize nickname
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = isMe ? Color.FromArgb(88, 101, 242) : Color.FromArgb(35, 165, 90); // Blurple for me, green for peer
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
            rtbChat.AppendText($"{sender}: ");

            // Message text formatting
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.FromArgb(219, 222, 225); // Off-white message text
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.AppendText($"{text}{Environment.NewLine}");

            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.ScrollToCaret();
        }

        private async void btnSend_Click(object? sender, EventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            // Stop sending typing status
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

        // Typing updates
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

        // --- File Send Activation ---

        private async void btnSendFile_Click(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Select File to Send";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = ofd.FileName;
                    try
                    {
                        // Connect socket streaming
                        await _chatService.FileTransferService.StartSendSessionAsync(
                            filePath, 
                            _peerId, 
                            _peer.Username, 
                            _peer.IpAddress, 
                            async (transferId, dynamicPort) =>
                            {
                                // Send file request proposal via TCP
                                return await _chatService.SendFileRequestAsync(_peerId, filePath, dynamicPort, transferId);
                            });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to initiate file transfer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // --- Emoji Picker Popup ---

        private void btnEmoji_Click(object? sender, EventArgs e)
        {
            ContextMenuStrip emojiMenu = new ContextMenuStrip();
            emojiMenu.BackColor = Color.FromArgb(43, 45, 49);
            emojiMenu.ForeColor = Color.White;

            string[] emojis = { "😀", "😂", "❤️", "👍", "🔥", "😎", "👋", "🎉", "😱", "😢" };

            foreach (var emoji in emojis)
            {
                var item = new ToolStripMenuItem(emoji)
                {
                    ForeColor = Color.White
                };
                item.Click += (s, ev) =>
                {
                    txtMessage.AppendText(emoji);
                    txtMessage.Focus();
                };
                emojiMenu.Items.Add(item);
            }

            emojiMenu.Show(btnEmoji, new Point(0, -emojiMenu.Height));
        }

        // --- Socket Events Handling ---

        private void ChatService_PrivateMessageReceived(string senderId, ChatMessage msg)
        {
            if (senderId == _peerId)
            {
                AppendChatMessage(msg.SenderUsername, msg.Text, msg.FormattedTimestamp, false);
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

        // --- File Progress Events Handling ---

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
                
                // Hide panel after 3 seconds
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
                e.SuppressKeyPress = true; // Prevent system ding and newline injection
                btnSend_Click(this, EventArgs.Empty);
            }
        }
    }
}
