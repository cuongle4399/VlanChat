using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
            Color.FromArgb(88, 101, 242),
            Color.FromArgb(237, 66, 69),
            Color.FromArgb(240, 167, 4),
            Color.FromArgb(35, 165, 90),
            Color.FromArgb(155, 89, 182),
            Color.FromArgb(233, 30, 99),
            Color.FromArgb(26, 188, 156),
            Color.FromArgb(52, 152, 219)
        };
        private readonly Dictionary<string, int> _unreadPrivateCounts = new();
        private string? _activeChatPeerId = null;
        private bool _isUpdatingSelection = false;
        private readonly List<FileLinkRange> _fileLinks = new();
        private Button? btnViewProfile = null;

[DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_VSCROLL = 0x0115;
        private const int SB_BOTTOM = 7;

        private sealed class FileLinkRange
        {
            public int Start { get; init; }
            public int Length { get; init; }
            public string FileId { get; init; } = string.Empty;

            public bool Contains(int index) => index >= Start && index < Start + Length;
        }

        public MainForm()
        {
            InitializeComponent();

btnViewProfile = new Button
            {
                Text = "Xem hồ sơ",
                BackColor = Color.FromArgb(88, 101, 242),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Size = new Size(120, 32),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnViewProfile.FlatAppearance.BorderSize = 0;
            btnViewProfile.Click += BtnViewProfile_Click;
            pnlChatHeader.Controls.Add(btnViewProfile);
            btnViewProfile.BringToFront();

pnlChatHeader.Resize += (s, ev) =>
            {
                if (btnViewProfile != null)
                {
                    btnViewProfile.Location = new Point(pnlChatHeader.Width - 140, (pnlChatHeader.Height - btnViewProfile.Height) / 2);
                }
            };

            _chatService = new ChatService();

UpdateUserProfileDisplay();

lstPeers.DrawMode = DrawMode.OwnerDrawFixed;
            lstPeers.ItemHeight = ScaleForDpi(52);
            lstPeers.IntegralHeight = false;
            lstPeers.DrawItem += LstPeers_DrawItem;
            lblMyName.AutoEllipsis = true;
            lblHeaderDetails.AutoEllipsis = true;

rtbChat.DetectUrls = true;
            rtbChat.LinkClicked += RtbChat_LinkClicked;
            rtbChat.MouseMove += RtbChat_MouseMove;
            rtbChat.MouseDown += RtbChat_MouseDown;

_chatService.PeerOnline += ChatService_PeerOnline;
            _chatService.PeerOffline += ChatService_PeerOffline;
            _chatService.PeerUpdated += ChatService_PeerUpdated;
            _chatService.PeerTypingChanged += ChatService_PeerTypingChanged;
            _chatService.GroupMessageReceived += ChatService_GroupMessageReceived;
            _chatService.PrivateMessageReceived += ChatService_PrivateMessageReceived;

_chatService.FileTransferService.TransferUpdated += Transfer_Updated;
            _chatService.FileTransferService.TransferCompleted += Transfer_Completed;
            _chatService.FileTransferService.TransferFailed += Transfer_Failed;

lstPeers.SelectedIndexChanged += lstPeers_SelectedIndexChanged;
            lstChannels.SelectedIndexChanged += lstChannels_SelectedIndexChanged;

lstChannels.SelectedIndex = 0;

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

        private void UpdateGroupHeaderDetails()
        {
            int onlineCount = _chatService.DiscoveredPeers.Values.Count(peer => peer.Status != "offline");
            lblHeaderTitle.Text = "# Chat nhóm LAN";
            lblHeaderDetails.Text = $"Gửi tới {onlineCount} người online | My Node: {_chatService.LocalIp}:{_chatService.ConfigManager.Config.Port}";
            if (btnViewProfile != null)
            {
                btnViewProfile.Visible = false;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

try
            {
                _chatService.Start();
                UpdateGroupHeaderDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize local network service: {ex.Message}", "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _chatService.PeerOnline -= ChatService_PeerOnline;
            _chatService.PeerOffline -= ChatService_PeerOffline;
            _chatService.PeerUpdated -= ChatService_PeerUpdated;
            _chatService.PeerTypingChanged -= ChatService_PeerTypingChanged;
            _chatService.GroupMessageReceived -= ChatService_GroupMessageReceived;
            _chatService.PrivateMessageReceived -= ChatService_PrivateMessageReceived;

            _chatService.FileTransferService.TransferUpdated -= Transfer_Updated;
            _chatService.FileTransferService.TransferCompleted -= Transfer_Completed;
            _chatService.FileTransferService.TransferFailed -= Transfer_Failed;

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

private void LstPeers_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstPeers.Items.Count)
                return;

            if (lstPeers.Items[e.Index] is not PeerInfo peer)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color bg = isSelected ? Color.FromArgb(64, 66, 73) : Color.FromArgb(43, 45, 49);
            using (Brush bgBrush = new SolidBrush(bg))
            {
                g.FillRectangle(bgBrush, e.Bounds);
            }

int avatarSize = ScaleForDpi(36);
            int avatarX = e.Bounds.X + ScaleForDpi(8);
            int avatarY = e.Bounds.Y + (e.Bounds.Height - avatarSize) / 2;

            Color avatarColor = _avatarColors[Math.Clamp(peer.AvatarIndex, 0, 7)];
            using (Brush avBrush = new SolidBrush(avatarColor))
            {
                g.FillEllipse(avBrush, avatarX, avatarY, avatarSize, avatarSize);
            }

char letter = string.IsNullOrEmpty(peer.Username) ? 'U' : peer.Username[0];
            using (Font letterFont = new Font("Segoe UI", 12F, FontStyle.Bold))
            using (Brush letterBrush = new SolidBrush(Color.White))
            {
                string letterStr = letter.ToString().ToUpper();
                SizeF size = g.MeasureString(letterStr, letterFont);
                g.DrawString(letterStr, letterFont, letterBrush, avatarX + (avatarSize - size.Width) / 2, avatarY + (avatarSize - size.Height) / 2);
            }

Color statusColor = Color.Gray;
            if (peer.IsTyping)
            {
                statusColor = Color.FromArgb(240, 167, 4);
            }
            else if (peer.Status == "online")
            {
                statusColor = Color.FromArgb(35, 165, 90);
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

using (Font nameFont = new Font("Segoe UI", 9.75F, FontStyle.Bold))
            using (Font detailsFont = new Font("Segoe UI", 8.25F, FontStyle.Regular))
            {
                _unreadPrivateCounts.TryGetValue(peer.Id, out int unreadCount);
                string name = peer.Username;
                if (peer.IsTyping)
                {
                    name += " (\u0111ang nh\u1eadp...)";
                }

                int textX = e.Bounds.X + ScaleForDpi(52);
                int reservedBadgeWidth = unreadCount > 0 ? ScaleForDpi(unreadCount > 99 ? 44 : 34) : 0;
                int textWidth = Math.Max(1, e.Bounds.Right - textX - ScaleForDpi(8) - reservedBadgeWidth);
                var nameBounds = new Rectangle(textX, e.Bounds.Y + ScaleForDpi(6), textWidth, ScaleForDpi(22));
                var detailsBounds = new Rectangle(textX, e.Bounds.Y + ScaleForDpi(28), textWidth, ScaleForDpi(18));
                var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

                TextRenderer.DrawText(g, name, nameFont, nameBounds, Color.White, flags);
                string detailsText = $"{peer.MachineName} ({peer.IpAddress}:{peer.Port})";
                if (peer.Status != "offline")
                {
                    detailsText += $" • {peer.OnlineSince:HH:mm}";
                }
                TextRenderer.DrawText(g, detailsText, detailsFont, detailsBounds, Color.FromArgb(148, 155, 164), flags);

                if (unreadCount > 0)
                {
                    DrawUnreadBadge(g, e.Bounds, unreadCount);
                }
            }
        }

        private void DrawUnreadBadge(Graphics g, Rectangle itemBounds, int count)
        {
            string text = count > 99 ? "99+" : count.ToString();
            int height = ScaleForDpi(22);
            int width = Math.Max(height, TextRenderer.MeasureText(text, Font).Width + ScaleForDpi(12));
            int x = itemBounds.Right - width - ScaleForDpi(10);
            int y = itemBounds.Y + (itemBounds.Height - height) / 2;
            var badgeRect = new Rectangle(x, y, width, height);

            using (Brush badgeBrush = new SolidBrush(Color.FromArgb(237, 66, 69)))
            using (Font badgeFont = new Font("Segoe UI", 8.25F, FontStyle.Bold))
            {
                g.FillEllipse(badgeBrush, badgeRect);
                TextRenderer.DrawText(
                    g,
                    text,
                    badgeFont,
                    badgeRect,
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

private void LoadGroupHistory()
        {
            rtbChat.Clear();
            _fileLinks.Clear();
            var messages = _chatService.HistoryService.GetGroupHistory();
            foreach (var msg in messages)
            {
                if (!string.IsNullOrWhiteSpace(msg.FilePath) && msg.FileSize > 0)
                {
                    AppendFileMessage(msg, msg.SenderId == _chatService.MyId);
                }
                else
                {
                    AppendChatMessage(msg.SenderUsername, msg.Text, msg.FormattedTimestamp, msg.SenderId == _chatService.MyId);
                }
            }
            ScrollChatToBottom();
        }

        private void ScrollChatToBottom()
        {
            if (rtbChat.IsDisposed || !rtbChat.IsHandleCreated) return;

            if (rtbChat.InvokeRequired)
            {
                BeginInvokeIfReady(ScrollChatToBottom);
                return;
            }

rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionLength = 0;

SendMessage(rtbChat.Handle, WM_VSCROLL, (IntPtr)SB_BOTTOM, IntPtr.Zero);
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

            ScrollChatToBottom();
        }

private async void btnSend_Click(object? sender, EventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            txtMessage.Clear();

            if (_activeChatPeerId == null)
            {

                bool delivered = await _chatService.SendGroupMessageAsync(text);
                AppendChatMessage(_chatService.ConfigManager.Config.Username, text, DateTime.Now.ToString("HH:mm:ss"), true);
                if (!delivered)
                {
                    AppendChatMessage("System", "Some peers could not receive the message.", DateTime.Now.ToString("HH:mm:ss"), false);
                }
            }
            else
            {

                bool delivered = await _chatService.SendPrivateMessageAsync(_activeChatPeerId, text);
                if (delivered)
                {
                    AppendChatMessage(_chatService.ConfigManager.Config.Username, text, DateTime.Now.ToString("HH:mm:ss"), true);
                }
                else
                {
                    AppendChatMessage("System", "Failed to deliver message. The user may have dropped offline.", DateTime.Now.ToString("HH:mm:ss"), false);
                }
            }
        }

        private void btnEmoji_Click(object? sender, EventArgs e)
        {

            ToolStripDropDown dropDown = new ToolStripDropDown();
            dropDown.Padding = Padding.Empty;
            dropDown.Margin = Padding.Empty;
            dropDown.AutoClose = true;

FlowLayoutPanel flowPanel = new FlowLayoutPanel
            {
                Width = 320,
                Height = 220,
                BackColor = Color.FromArgb(43, 45, 49),
                Padding = new Padding(5),
                AutoScroll = true
            };

            string[] emojis = {
                "🙂", "😀", "😄", "😆", "😅", "😂", "🤣", "😊", "☺️", "😌", "😉", "😏", "😍", "😘", "😗", "😙",
                "😚", "🤗", "😳", "🙃", "😇", "😈", "😛", "😝", "😜", "😋", "🤤", "🤓", "😎", "🤑", "😒", "🙁",
                "☹️", "😞", "😔", "😖", "😓", "😢", "😭", "😟", "😣", "😩", "😫", "😕", "🤔", "🙄",
                "😤", "😠", "😡", "😶", "🤐", "😐", "😑", "😯", "😲", "😧", "😧", "😨", "😰", "😱", "😪", "😴",
                "😬", "🤥", "🤧", "🤒", "😷", "🤕", "😵", "🤢", "🤠", "🤡", "👿", "👹", "👺", "👻", "💀", "👽",
                "👾", "🤖", "💩", "🎃", "👍", "👎", "❤️", "🔥", "🎉", "👋"
            };

            foreach (var emoji in emojis)
            {
                Button btn = new Button
                {
                    Text = emoji,
                    Size = new Size(32, 32),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI Emoji", 12F, FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(2)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 66, 73);
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(88, 101, 242);

                btn.Click += (s, ev) =>
                {
                    txtMessage.AppendText(emoji);
                    txtMessage.Focus();
                    dropDown.Close();
                };

                flowPanel.Controls.Add(btn);
            }

            ToolStripControlHost host = new ToolStripControlHost(flowPanel);
            host.Padding = Padding.Empty;
            host.Margin = Padding.Empty;
            host.AutoSize = false;
            host.Size = flowPanel.Size;
            dropDown.Items.Add(host);

            dropDown.Show(btnEmoji, new Point(0, -flowPanel.Height));
        }

        private async void btnSendFile_Click(object? sender, EventArgs e)
        {
            if (_activeChatPeerId != null)
            {
                if (_chatService.DiscoveredPeers.TryGetValue(_activeChatPeerId, out var peer))
                {
                    await SendFileToPeerAsync(peer);
                }
                return;
            }

using OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Chọn file gửi lên nhóm chung"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                await SendFileToGroupAsync(ofd.FileName);
            }
        }

        private async Task SendFileToGroupAsync(string filePath)
        {
            try
            {
                await _chatService.FileTransferService.StartSendSessionAsync(
                    filePath,
                    "group",
                    "Nhóm chung",
                    "255.255.255.255",
                    async (transferId, dynamicPort) =>
                    {
                        return await _chatService.SendGroupFileRequestAsync(filePath, dynamicPort, transferId);
                    });

                AppendChatMessage("Hệ thống", $"Đang chuẩn bị gửi file '{Path.GetFileName(filePath)}' lên phòng chat nhóm...", DateTime.Now.ToString("HH:mm:ss"), false);
                LoadGroupHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể chia sẻ file nhóm: {ex.Message}", "Gửi file nhóm", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SendFileToPeerAsync(PeerInfo peer)
        {
            if (!_chatService.DiscoveredPeers.TryGetValue(peer.Id, out var currentPeer) || currentPeer.Status == "offline")
            {
                MessageBox.Show("Peer này hiện không online.", "Gửi file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using OpenFileDialog ofd = new OpenFileDialog
            {
                Title = $"Chọn file gửi cho {currentPeer.Username}"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            string filePath = ofd.FileName;
            try
            {
                await _chatService.FileTransferService.StartSendSessionAsync(
                    filePath,
                    currentPeer.Id,
                    currentPeer.Username,
                    currentPeer.IpAddress,
                    async (transferId, dynamicPort) =>
                    {
                        return await _chatService.SendFileRequestAsync(currentPeer.Id, filePath, dynamicPort, transferId);
                    });

                AppendChatMessage("Hệ thống", $"Đã gửi yêu cầu truyền file '{Path.GetFileName(filePath)}' tới {currentPeer.Username}.", DateTime.Now.ToString("HH:mm:ss"), false);
                OpenPrivateChat(currentPeer.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể gửi file: {ex.Message}", "Gửi file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSettings_Click(object? sender, EventArgs e)
        {
            using (var settings = new SettingsForm(_chatService))
            {
                if (settings.ShowDialog() == DialogResult.OK)
                {
                    UpdateUserProfileDisplay();
                    UpdateGroupHeaderDetails();
                }
            }
        }

private void OpenPrivateChat(string peerId)
        {
            ClearUnreadForPeer(peerId);

foreach (var item in lstPeers.Items)
            {
                if (item is PeerInfo peer && peer.Id == peerId)
                {
                    lstPeers.SelectedItem = peer;
                    break;
                }
            }
        }

        private bool IsPrivateChatActive(string peerId)
        {

            return _activeChatPeerId == peerId;
        }

        private void AddUnreadForPeer(string peerId)
        {
            _unreadPrivateCounts.TryGetValue(peerId, out int current);
            _unreadPrivateCounts[peerId] = Math.Min(current + 1, 999);
            lstPeers.Invalidate();
        }

        private void ClearUnreadForPeer(string peerId)
        {
            if (_unreadPrivateCounts.Remove(peerId))
            {
                lstPeers.Invalidate();
            }
        }

        private void lstPeers_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingSelection) return;

            if (lstPeers.SelectedItem is PeerInfo peer)
            {
                _isUpdatingSelection = true;
                lstChannels.SelectedIndex = -1;
                _isUpdatingSelection = false;

                SwitchToChat(peer.Id);
            }
        }

        private void lstChannels_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingSelection) return;

            if (lstChannels.SelectedIndex != -1)
            {
                _isUpdatingSelection = true;
                lstPeers.SelectedIndex = -1;
                _isUpdatingSelection = false;

                SwitchToChat(null);
            }
        }

        private void SwitchToChat(string? peerId)
        {
            _activeChatPeerId = peerId;

            if (peerId == null)
            {

                UpdateGroupHeaderDetails();
                LoadGroupHistory();
                if (btnViewProfile != null)
                {
                    btnViewProfile.Visible = false;
                }
            }
            else
            {

                ClearUnreadForPeer(peerId);
                UpdatePrivateHeaderDetails(peerId);
                LoadPrivateHistory(peerId);
            }
        }

        private void UpdatePrivateHeaderDetails(string peerId)
        {
            if (_chatService.DiscoveredPeers.TryGetValue(peerId, out var peer))
            {
                lblHeaderTitle.Text = $"{peer.Username} ({peer.IpAddress}:{peer.Port})";
                lblHeaderDetails.Text = $"Trạng thái: {(peer.Status == "online" ? "🟢 Trực tuyến" : "⚫ Ngoại tuyến")}";
                if (btnViewProfile != null)
                {
                    btnViewProfile.Visible = true;
                }
            }
        }

        private void BtnViewProfile_Click(object? sender, EventArgs e)
        {
            if (_activeChatPeerId != null)
            {
                OpenPeerDetails(_activeChatPeerId);
            }
        }

        private void LoadPrivateHistory(string peerId)
        {
            rtbChat.Clear();
            _fileLinks.Clear();
            var messages = _chatService.HistoryService.GetPrivateHistory(peerId);
            foreach (var msg in messages)
            {
                if (!string.IsNullOrWhiteSpace(msg.FilePath) && msg.FileSize > 0)
                {
                    AppendFileMessage(msg, msg.SenderId == _chatService.MyId);
                }
                else
                {
                    AppendChatMessage(msg.SenderUsername, msg.Text, msg.FormattedTimestamp, msg.SenderId == _chatService.MyId);
                }
            }
            ScrollChatToBottom();
        }

        private void lstPeers_DoubleClick(object sender, EventArgs e)
        {
            if (lstPeers.SelectedItem is PeerInfo peer)
            {
                OpenPrivateChat(peer.Id);
            }
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
            rtbChat.AppendText("[File] ");

rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.White;
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Underline | FontStyle.Bold);
            rtbChat.AppendText(fileName);

rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionColor = Color.FromArgb(148, 155, 164);
            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.AppendText($" ({Helpers.FormatFileSize(msg.FileSize)})  —  ");

if (_chatService.FileTransferService.Transfers.TryGetValue(msg.Id, out var transfer))
            {
                if (transfer.Status == FileTransferStatus.Pending)
                {
                    rtbChat.SelectionStart = rtbChat.TextLength;
                    rtbChat.SelectionColor = Color.FromArgb(255, 179, 102);
                    rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Italic);
                    if (transfer.Direction == FileTransferDirection.Send)
                    {
                        if (transfer.PeerId == "group")
                        {
                            rtbChat.AppendText("Đã chia sẻ");
                        }
                        else
                        {
                            rtbChat.AppendText("Chờ nhận...");
                        }
                    }
                    else
                    {
                        rtbChat.AppendText("Đang chuẩn bị...");
                    }
                }
                else if (transfer.Status == FileTransferStatus.Transferring)
                {
                    int percent = Math.Clamp((int)transfer.ProgressPercentage, 0, 100);
                    rtbChat.SelectionStart = rtbChat.TextLength;
                    rtbChat.SelectionColor = Color.FromArgb(88, 166, 255);
                    rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
                    rtbChat.AppendText(isMe ? $"Đang gửi ({percent}%)" : $"Đang nhận ({percent}%)");
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
                    rtbChat.AppendText("Lỗi truyền tải");
                }
            }
            else
            {
                AppendCompletedFileStatus(msg, isMe);
            }

            rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Regular);
            rtbChat.AppendText(Environment.NewLine);
            ScrollChatToBottom();
        }

        private void AppendCompletedFileStatus(ChatMessage msg, bool isMe)
        {
            if (isMe)
            {
                rtbChat.SelectionStart = rtbChat.TextLength;
                rtbChat.SelectionColor = Color.FromArgb(46, 204, 113);
                rtbChat.SelectionFont = new Font(rtbChat.Font, FontStyle.Bold);
                rtbChat.AppendText("Đã gửi");
            }
            else
            {
                int linkStart = rtbChat.TextLength;
                string linkText = File.Exists(msg.FilePath) ? "Mở thư mục" : "Tải xuống";
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

        private void RtbChat_LinkClicked(object? sender, LinkClickedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.LinkText))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = e.LinkText,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open URL link", ex);
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
            ChatMessage? msg = null;
            if (_activeChatPeerId == null)
            {
                var messages = _chatService.HistoryService.GetGroupHistory();
                msg = messages.FirstOrDefault(m => m.Id == fileId);
            }
            else
            {
                var messages = _chatService.HistoryService.GetPrivateHistory(_activeChatPeerId);
                msg = messages.FirstOrDefault(m => m.Id == fileId);
            }

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

private void Transfer_Updated(FileTransferInfo transfer)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (_activeChatPeerId == transfer.PeerId)
            {
                BeginInvokeIfReady(() =>
                {
                    LoadPrivateHistory(_activeChatPeerId);
                });
            }
            else if (_activeChatPeerId == null && transfer.PeerId == "group")
            {
                BeginInvokeIfReady(() =>
                {
                    LoadGroupHistory();
                });
            }
        }

        private void Transfer_Completed(FileTransferInfo transfer)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (_activeChatPeerId == transfer.PeerId)
            {
                BeginInvokeIfReady(() =>
                {
                    LoadPrivateHistory(_activeChatPeerId);
                });
            }
            else if (_activeChatPeerId == null && transfer.PeerId == "group")
            {
                BeginInvokeIfReady(() =>
                {
                    LoadGroupHistory();
                });
            }
        }

        private void Transfer_Failed(FileTransferInfo transfer, string reason)
        {
            if (this.IsDisposed || this.Disposing) return;
            if (_activeChatPeerId == transfer.PeerId)
            {
                BeginInvokeIfReady(() =>
                {
                    LoadPrivateHistory(_activeChatPeerId);
                });
            }
            else if (_activeChatPeerId == null && transfer.PeerId == "group")
            {
                BeginInvokeIfReady(() =>
                {
                    LoadGroupHistory();
                });
            }
        }

        private void lstPeers_MouseDown(object sender, MouseEventArgs e)
        {
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

private void ChatService_PeerOnline(PeerInfo peer)
        {
            BeginInvokeIfReady(() =>
            {
                RefreshPeersList();
                if (_activeChatPeerId == null)
                {
                    UpdateGroupHeaderDetails();
                }
                else
                {
                    UpdatePrivateHeaderDetails(_activeChatPeerId);
                }
            });
        }

        private void ChatService_PeerOffline(PeerInfo peer)
        {
            BeginInvokeIfReady(() =>
            {
                RefreshPeersList();
                if (_activeChatPeerId == null)
                {
                    UpdateGroupHeaderDetails();
                }
                else
                {
                    UpdatePrivateHeaderDetails(_activeChatPeerId);
                }
            });
        }

        private void ChatService_PeerUpdated(PeerInfo peer)
        {
            BeginInvokeIfReady(() =>
            {
                if (_activeChatPeerId == null)
                {
                    UpdateGroupHeaderDetails();
                }
                else
                {
                    UpdatePrivateHeaderDetails(_activeChatPeerId);
                }
                lstPeers.Invalidate();
            });
        }

        private void ChatService_PeerTypingChanged(PeerInfo peer, bool isTyping)
        {
            BeginInvokeIfReady(() => lstPeers.Invalidate());
        }

        private void ChatService_GroupMessageReceived(ChatMessage msg)
        {
            BeginInvokeIfReady(() =>
            {
                if (_activeChatPeerId == null)
                {
                    if (!string.IsNullOrWhiteSpace(msg.FilePath) && msg.FileSize > 0)
                    {
                        AppendFileMessage(msg, false);
                    }
                    else
                    {
                        AppendChatMessage(msg.SenderUsername, msg.Text, msg.FormattedTimestamp, false);
                    }
                }
            });
        }

        private void ChatService_PrivateMessageReceived(string senderId, ChatMessage msg)
        {
            BeginInvokeIfReady(() =>
            {
                if (_activeChatPeerId == senderId)
                {
                    ClearUnreadForPeer(senderId);
                    if (!string.IsNullOrWhiteSpace(msg.FilePath) && msg.FileSize > 0)
                    {
                        AppendFileMessage(msg, false);
                    }
                    else
                    {
                        AppendChatMessage(msg.SenderUsername, msg.Text, msg.FormattedTimestamp, false);
                    }
                    return;
                }

                AddUnreadForPeer(senderId);
            });
        }

        private void RefreshPeersList()
        {
            lstPeers.BeginUpdate();
            try
            {
                lstPeers.Items.Clear();

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

            if (_activeChatPeerId == null)
            {
                UpdateGroupHeaderDetails();
            }
        }

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
                e.SuppressKeyPress = true;
                btnSend_Click(this, EventArgs.Empty);
            }
        }
    }
}
