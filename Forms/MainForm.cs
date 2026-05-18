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
    public partial class MainForm : Form
    {
        private readonly ChatService _chatService;
        private readonly Color[] _avatarColors = {
            Color.FromArgb(88, 101, 242),  // Discord Blurple
            Color.FromArgb(237, 66, 69),   // Crimson Red
            Color.FromArgb(240, 167, 4),    // Sun Golden Yellow
            Color.FromArgb(35, 165, 90),   // Emerald Green
            Color.FromArgb(155, 89, 182),  // Amethyst Purple
            Color.FromArgb(233, 30, 99),   // Deep Hot Pink
            Color.FromArgb(26, 188, 156),  // Teal Turquoise
            Color.FromArgb(52, 152, 219)   // Sky Blue
        };

        public MainForm()
        {
            InitializeComponent();

            _chatService = new ChatService();

            // Setup current user profile display
            UpdateUserProfileDisplay();

            // Set ListBox owner draw configurations
            lstPeers.DrawMode = DrawMode.OwnerDrawFixed;
            lstPeers.ItemHeight = ScaleForDpi(52);
            lstPeers.IntegralHeight = false;
            lstPeers.DrawItem += LstPeers_DrawItem;
            lblMyName.AutoEllipsis = true;
            lblHeaderDetails.AutoEllipsis = true;

            // Bind async network core events
            _chatService.PeerOnline += ChatService_PeerOnline;
            _chatService.PeerOffline += ChatService_PeerOffline;
            _chatService.PeerUpdated += ChatService_PeerUpdated;
            _chatService.PeerTypingChanged += ChatService_PeerTypingChanged;
            _chatService.GroupMessageReceived += ChatService_GroupMessageReceived;
            _chatService.PrivateMessageReceived += ChatService_PrivateMessageReceived;
            _chatService.FileRequestReceived += ChatService_FileRequestReceived;

            // Load local public channel logs
            LoadGroupHistory();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            lstPeers.ItemHeight = ScaleForDpi(52);
            UpdateUserProfileDisplay();
            lstPeers.Invalidate();
        }

        private int ScaleForDpi(int value)
        {
            return (int)Math.Round(value * (DeviceDpi / 96.0));
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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // Activate background server and broadcast discovery sockets
            try
            {
                _chatService.Start();
                lblHeaderDetails.Text = $"My Node: {_chatService.LocalIp}:{_chatService.ConfigManager.Config.Port}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize local network service: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _chatService.Stop();
            base.OnFormClosing(e);
        }

        private void UpdateUserProfileDisplay()
        {
            lblMyName.Text = _chatService.ConfigManager.Config.Username;
            Image? oldImage = picMyAvatar.Image;
            picMyAvatar.Image = CreateCircularAvatar(
                _chatService.ConfigManager.Config.Username, 
                _chatService.ConfigManager.Config.AvatarIndex, 
                picMyAvatar.Width, 
                picMyAvatar.Height
            );
            oldImage?.Dispose();
        }

        private Image CreateCircularAvatar(string username, int avatarIndex, int w, int h)
        {
            w = Math.Max(1, w);
            h = Math.Max(1, h);
            Bitmap bmp = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Color baseColor = _avatarColors[Math.Clamp(avatarIndex, 0, 7)];
                
                using (Brush brush = new SolidBrush(baseColor))
                {
                    g.FillEllipse(brush, 0, 0, w, h);
                }

                char letter = string.IsNullOrEmpty(username) ? 'U' : username[0];
                using (Font font = new Font("Segoe UI", (float)(w * 0.45), FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    string letterStr = letter.ToString().ToUpper();
                    SizeF size = g.MeasureString(letterStr, font);
                    g.DrawString(letterStr, font, textBrush, (w - size.Width) / 2, (h - size.Height) / 2);
                }
            }
            return bmp;
        }

        // --- Custom Owner Draw Peer Item ---

        private void LstPeers_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstPeers.Items.Count)
                return;

            if (lstPeers.Items[e.Index] is not PeerInfo peer)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Highlight hover and selection background
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bg = isSelected ? Color.FromArgb(64, 66, 73) : Color.FromArgb(43, 45, 49); // Slate hover colors
            using (Brush bgBrush = new SolidBrush(bg))
            {
                g.FillRectangle(bgBrush, e.Bounds);
            }

            // Draw Circular Profile Photo
            int avatarSize = ScaleForDpi(36);
            int avatarX = e.Bounds.X + ScaleForDpi(8);
            int avatarY = e.Bounds.Y + (e.Bounds.Height - avatarSize) / 2;

            Color avatarColor = _avatarColors[Math.Clamp(peer.AvatarIndex, 0, 7)];
            using (Brush avBrush = new SolidBrush(avatarColor))
            {
                g.FillEllipse(avBrush, avatarX, avatarY, avatarSize, avatarSize);
            }

            // Draw Character Initials inside Profile
            char letter = string.IsNullOrEmpty(peer.Username) ? 'U' : peer.Username[0];
            using (Font letterFont = new Font("Segoe UI", 12F, FontStyle.Bold))
            using (Brush letterBrush = new SolidBrush(Color.White))
            {
                string letterStr = letter.ToString().ToUpper();
                SizeF size = g.MeasureString(letterStr, letterFont);
                g.DrawString(letterStr, letterFont, letterBrush, avatarX + (avatarSize - size.Width) / 2, avatarY + (avatarSize - size.Height) / 2);
            }

            // Draw Status Dot
            Color statusColor = Color.Gray;
            if (peer.IsTyping)
            {
                statusColor = Color.FromArgb(240, 167, 4); // Amber Typing Alert
            }
            else if (peer.Status == "online")
            {
                statusColor = Color.FromArgb(35, 165, 90); // Green Online status
            }

            int statusSize = ScaleForDpi(10);
            int statusX = avatarX + avatarSize - statusSize + ScaleForDpi(2);
            int statusY = avatarY + avatarSize - statusSize + ScaleForDpi(2);
            using (Brush stBrush = new SolidBrush(statusColor))
            using (Pen stPen = new Pen(Color.FromArgb(43, 45, 49), Math.Max(1, ScaleForDpi(2))))
            {
                g.FillEllipse(stBrush, statusX, statusY, statusSize, statusSize);
                g.DrawEllipse(stPen, statusX, statusY, statusSize, statusSize);
            }

            // Text Rendering (Username, IP, OS name)
            using (Font nameFont = new Font("Segoe UI", 9.75F, FontStyle.Bold))
            using (Font detailsFont = new Font("Segoe UI", 8.25F, FontStyle.Regular))
            {
                string name = peer.Username;
                if (peer.IsTyping)
                {
                    name += " (\u0111ang nh\u1eadp...)";
                }

                int textX = e.Bounds.X + ScaleForDpi(52);
                int textWidth = Math.Max(1, e.Bounds.Right - textX - ScaleForDpi(8));
                var nameBounds = new Rectangle(textX, e.Bounds.Y + ScaleForDpi(6), textWidth, ScaleForDpi(22));
                var detailsBounds = new Rectangle(textX, e.Bounds.Y + ScaleForDpi(28), textWidth, ScaleForDpi(18));
                var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

                TextRenderer.DrawText(g, name, nameFont, nameBounds, Color.White, flags);
                TextRenderer.DrawText(g, $"{peer.MachineName} ({peer.IpAddress})", detailsFont, detailsBounds, Color.FromArgb(148, 155, 164), flags);
            }
        }

        // --- Channel History Loading & Appends ---

        private void LoadGroupHistory()
        {
            rtbChat.Clear();
            var messages = _chatService.HistoryService.GetGroupHistory();
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
            rtbChat.SelectionStart = start;
            rtbChat.SelectionColor = Color.FromArgb(148, 155, 164); // Timestamp
            rtbChat.AppendText($"[{time}] ");

            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = isMe ? Color.FromArgb(88, 101, 242) : Color.FromArgb(35, 165, 90);
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
            rtbChat.AppendText($"{sender}: ");

            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.FromArgb(219, 222, 225); // Message text
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.AppendText($"{text}{Environment.NewLine}");

            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.ScrollToCaret();
        }

        // --- Core UI Action Trigger Actions ---

        private async void btnSend_Click(object? sender, EventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            txtMessage.Clear();
            bool delivered = await _chatService.SendGroupMessageAsync(text);

            // Append our own sent message to the general chat window immediately in real-time
            AppendChatMessage(_chatService.ConfigManager.Config.Username, text, DateTime.Now.ToString("HH:mm:ss"), true);
            if (!delivered)
            {
                AppendChatMessage("System", "Some peers could not receive the message.", DateTime.Now.ToString("HH:mm:ss"), false);
            }
        }

        private void btnEmoji_Click(object? sender, EventArgs e)
        {
            ContextMenuStrip emojiMenu = new ContextMenuStrip();
            emojiMenu.BackColor = Color.FromArgb(43, 45, 49);
            emojiMenu.ForeColor = Color.White;

            string[] emojis = { "😀", "😂", "❤️", "👍", "🔥", "😎", "👋", "🎉", "😱", "😢" };

            foreach (var emoji in emojis)
            {
                var item = new ToolStripMenuItem(emoji) { ForeColor = Color.White };
                item.Click += (s, ev) =>
                {
                    txtMessage.AppendText(emoji);
                    txtMessage.Focus();
                };
                emojiMenu.Items.Add(item);
            }

            emojiMenu.Show(btnEmoji, new Point(0, -emojiMenu.Height));
        }

        private void btnSettings_Click(object? sender, EventArgs e)
        {
            using (var settings = new SettingsForm(_chatService))
            {
                if (settings.ShowDialog() == DialogResult.OK)
                {
                    UpdateUserProfileDisplay();
                }
            }
        }

        // --- Peer List Interaction Actions ---

        private void OpenPrivateChat(string peerId)
        {
            // If direct private dialog with peer is already open, focus it
            foreach (Form openForm in Application.OpenForms)
            {
                if (openForm is PrivateChatForm pcf && openForm.Tag is string tag && tag == peerId)
                {
                    pcf.Activate();
                    return;
                }
            }

            var chatForm = new PrivateChatForm(_chatService, peerId);
            chatForm.Tag = peerId;
            chatForm.Show();
        }

        private void lstPeers_DoubleClick(object sender, EventArgs e)
        {
            if (lstPeers.SelectedItem is PeerInfo peer)
            {
                OpenPrivateChat(peer.Id);
            }
        }

        private void lstPeers_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = lstPeers.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    lstPeers.SelectedIndex = index;
                    if (lstPeers.Items[index] is PeerInfo peer)
                    {
                        ContextMenuStrip menu = new ContextMenuStrip();
                        menu.BackColor = Color.FromArgb(43, 45, 49);
                        
                        var itemChat = new ToolStripMenuItem("Nhắn riêng") { ForeColor = Color.White };
                        itemChat.Click += (s, ev) => OpenPrivateChat(peer.Id);
                        menu.Items.Add(itemChat);

                        var itemDetails = new ToolStripMenuItem("Xem chi tiết") { ForeColor = Color.White };
                        itemDetails.Click += (s, ev) => OpenPeerDetails(peer.Id);
                        menu.Items.Add(itemDetails);

                        menu.Show(lstPeers, e.Location);
                    }
                }
            }
        }

        private void OpenPeerDetails(string peerId)
        {
            using (var detailsForm = new PeerDetailsForm(_chatService, peerId))
            {
                if (detailsForm.ShowDialog() == DialogResult.OK)
                {
                    OpenPrivateChat(peerId);
                }
            }
        }

        // --- Socket Events Handling ---

        private void ChatService_PeerOnline(PeerInfo peer)
        {
            BeginInvokeIfReady(() =>
            {
                RefreshPeersList();
                AppendChatMessage("Hệ thống", $"📢 {peer.Username} ({peer.MachineName}) đã tham gia mạng LAN.", DateTime.Now.ToString("HH:mm:ss"), false);
            });
        }

        private void ChatService_PeerOffline(PeerInfo peer)
        {
            BeginInvokeIfReady(() =>
            {
                RefreshPeersList();
                AppendChatMessage("Hệ thống", $"⚫ {peer.Username} ({peer.MachineName}) đã ngắt kết nối.", DateTime.Now.ToString("HH:mm:ss"), false);
            });
        }

        private void ChatService_PeerUpdated(PeerInfo peer)
        {
            BeginInvokeIfReady(() => lstPeers.Invalidate());
        }

        private void ChatService_PeerTypingChanged(PeerInfo peer, bool isTyping)
        {
            BeginInvokeIfReady(() => lstPeers.Invalidate());
        }

        private void ChatService_GroupMessageReceived(ChatMessage msg)
        {
            AppendChatMessage(msg.SenderUsername, msg.Text, msg.FormattedTimestamp, false);
        }

        private void ChatService_PrivateMessageReceived(string senderId, ChatMessage msg)
        {
            // Auto open dialogue form
            BeginInvokeIfReady(() =>
            {
                OpenPrivateChat(senderId);
            });
        }

        private void RefreshPeersList()
        {
            lstPeers.BeginUpdate();
            try
            {
                lstPeers.Items.Clear();

                // Order active users first, offline nodes later
                var peers = _chatService.DiscoveredPeers.Values
                    .OrderByDescending(p => p.Status == "online" || p.IsTyping)
                    .ThenBy(p => p.Username);

                foreach (var p in peers)
                {
                    lstPeers.Items.Add(p);
                }
            }
            finally
            {
                lstPeers.EndUpdate();
            }
        }

        // --- Asynchronous File stream triggers ---

        private void ChatService_FileRequestReceived(NetworkMessage msg, string senderIp)
        {
            BeginInvokeIfReady(async () =>
            {
                int filePort = msg.FilePort > 0 ? msg.FilePort : msg.SenderPort;
                string safeFileName = Path.GetFileName(msg.FileName);
                if (string.IsNullOrWhiteSpace(safeFileName))
                {
                    safeFileName = "received_file";
                }

                string sizeStr = Helpers.FormatFileSize(msg.FileSize);
                string prompt = $"{msg.SenderUsername} muốn gửi file cho bạn:{Environment.NewLine}" +
                                $"File: {safeFileName}{Environment.NewLine}" +
                                $"Dung lượng: {sizeStr}{Environment.NewLine}{Environment.NewLine}" +
                                "Bạn có muốn nhận file này không?";

                DialogResult result = MessageBox.Show(prompt, "Yêu cầu nhận file LAN", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    using (var sfd = new SaveFileDialog())
                    {
                        sfd.FileName = safeFileName;
                        sfd.InitialDirectory = _chatService.ConfigManager.Config.DownloadFolder;

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            string savePath = sfd.FileName;

                            // Bind receive stream
                            _chatService.FileTransferService.StartReceiveSession(
                                msg.FileId,
                                safeFileName,
                                msg.FileSize,
                                msg.SenderId,
                                msg.SenderUsername,
                                senderIp,
                                filePort,
                                savePath
                            );

                            // Pop private dialogue immediately to track stats
                            OpenPrivateChat(msg.SenderId);
                        }
                        else
                        {
                            await _chatService.SendFileRejectAsync(senderIp, msg.SenderPort, msg.FileId);
                        }
                    }
                }
                else
                {
                    await _chatService.SendFileRejectAsync(senderIp, msg.SenderPort, msg.FileId);
                }
            });
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true; // Prevent beep and double entry
                btnSend_Click(this, EventArgs.Empty);
            }
        }
    }
}
