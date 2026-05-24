using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using LANChatPro.Storage;
using LANChatPro.Services;
using LANChatPro.Utils;

namespace LANChatPro.Forms
{
    public partial class ConnectionCheckForm : Form
    {
        private readonly ConfigManager _configManager;

        public ConnectionCheckForm()
        {
            InitializeComponent();
            _configManager = new ConfigManager();

            // Load saved settings
            txtServerIp.Text = _configManager.Config.ServerIp;
            nudServerPort.Value = Math.Clamp(_configManager.Config.ServerPort, 1, 65535);
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Run verification check on load
            await StartCheckAsync();
        }

        private async Task StartCheckAsync()
        {
            string ip = txtServerIp.Text.Trim();
            int port = (int)nudServerPort.Value;

            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("Địa chỉ IP máy chủ không được để trống!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Disable controls during test
            SetControlsEnabled(false);
            lblStatus.Text = "Đang kiểm tra kết nối tới máy chủ...";
            lblStatus.ForeColor = Color.FromArgb(240, 167, 4); // Yellow
            lblError.Visible = false;

            // Attempt TCP connection check
            bool connected = await ChatService.CanConnectToServerAsync(ip, port, timeoutMs: 3000);

            if (connected)
            {
                lblStatus.Text = "Kết nối thành công!";
                lblStatus.ForeColor = Color.FromArgb(35, 165, 90); // Green
                await Task.Delay(500); // Small delay to let user see success

                // Save configuration if it has changed
                if (ip != _configManager.Config.ServerIp || port != _configManager.Config.ServerPort)
                {
                    _configManager.Config.ServerIp = ip;
                    _configManager.Config.ServerPort = port;
                    _configManager.Save();
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                // Enable controls so user can edit and retry
                SetControlsEnabled(true);
                lblStatus.Text = "Không thể kết nối tới máy chủ!";
                lblStatus.ForeColor = Color.FromArgb(237, 66, 69); // Red

                lblError.Text = $"Không tìm thấy LANChatServer tại {ip}:{port}.\nChắc chắn rằng máy chủ đang chạy và tường lửa không chặn cổng này.";
                lblError.Visible = true;
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            txtServerIp.Enabled = enabled;
            nudServerPort.Enabled = enabled;
            btnConnect.Enabled = enabled;
            btnSaveIp.Enabled = enabled;
            btnExit.Enabled = enabled;
        }

        private async void btnConnect_Click(object? sender, EventArgs e)
        {
            await StartCheckAsync();
        }

        private void btnSaveIp_Click(object? sender, EventArgs e)
        {
            string ip = txtServerIp.Text.Trim();
            int port = (int)nudServerPort.Value;

            if (string.IsNullOrEmpty(ip))
            {
                MessageBox.Show("Địa chỉ IP máy chủ không được để trống!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _configManager.Config.ServerIp = ip;
            _configManager.Config.ServerPort = port;
            _configManager.Save();

            MessageBox.Show($"Đã lưu địa chỉ máy chủ {ip}:{port} thành công!", "Lưu cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExit_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
