using System;
using System.Drawing;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LANChatPro.Models;
using LANChatPro.Services;
using LANChatPro.Utils;

namespace LANChatPro.Forms
{
    public partial class PeerDetailsForm : Form
    {
        private readonly ChatService _chatService;
        private readonly string _peerId;
        private PeerInfo _peer;

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

        public PeerDetailsForm(ChatService chatService, string peerId)
        {
            InitializeComponent();
            _chatService = chatService;
            _peerId = peerId;

            if (!_chatService.DiscoveredPeers.TryGetValue(peerId, out var peer))
            {
                throw new ArgumentException("Peer not found on LAN", nameof(peerId));
            }
            _peer = peer;

LoadPeerDetails();
        }

        private void LoadPeerDetails()
        {
            this.Text = $"Thông tin chi tiết: {_peer.Username}";
            lblUsername.Text = _peer.Username;
            lblMachineName.Text = _peer.MachineName;
            lblIpAddress.Text = _peer.IpAddress;
            lblPort.Text = _peer.Port.ToString();

picAvatar.Image = CreateCircularAvatar(_peer.Username, _peer.AvatarIndex, picAvatar.Width, picAvatar.Height);

if (_peer.Status == "online")
            {
                lblStatus.Text = "🟢 Online (Hoạt động)";
                lblStatus.ForeColor = Color.FromArgb(35, 165, 90);
            }
            else
            {
                lblStatus.Text = "⚫ Offline (Ngoại tuyến)";
                lblStatus.ForeColor = Color.FromArgb(148, 155, 164);
            }

            lblLastSeen.Text = _peer.LastSeen.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        }

        private Image CreateCircularAvatar(string username, int avatarIndex, int w, int h)
        {
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
                using (Font font = new Font("Segoe UI", (float)(w * 0.4), FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    string letterStr = letter.ToString().ToUpper();
                    SizeF size = g.MeasureString(letterStr, font);
                    g.DrawString(letterStr, font, textBrush, (w - size.Width) / 2, (h - size.Height) / 2);
                }
            }
            return bmp;
        }

        private async void btnPing_Click(object? sender, EventArgs e)
        {
            btnPing.Enabled = false;
            lblPingResult.Text = "Đang kiểm tra...";
            lblPingResult.ForeColor = Color.FromArgb(240, 167, 4);

            long ms = await MeasurePingAsync(_peer.IpAddress, _peer.Port);

            if (this.IsDisposed || this.Disposing) return;

            if (ms >= 0)
            {
                lblPingResult.Text = $"Kết nối tốt! Trễ mạng (RTT): {ms} ms";
                lblPingResult.ForeColor = Color.FromArgb(35, 165, 90);
            }
            else
            {
                lblPingResult.Text = "Không thể kết nối! Peer có thể đã offline hoặc bị chặn tường lửa.";
                lblPingResult.ForeColor = Color.FromArgb(237, 66, 69);
            }

            btnPing.Enabled = true;
        }

        private async Task<long> MeasurePingAsync(string ip, int port)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using (var client = new TcpClient())
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2500));
                    await client.ConnectAsync(ip, port, cts.Token);
                    sw.Stop();
                    return sw.ElapsedMilliseconds;
                }
            }
            catch
            {

            }
            return -1;
        }

        private void btnMessage_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnClose_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}
