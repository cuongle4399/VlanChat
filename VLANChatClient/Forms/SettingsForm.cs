using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LANChatPro.Services;

namespace LANChatPro.Forms
{
    public partial class SettingsForm : Form
    {
        private readonly ChatService _chatService;
        private int _selectedAvatarIndex;
        private readonly Button[] _avatarButtons = new Button[8];

        public SettingsForm(ChatService chatService)
        {
            InitializeComponent();
            _chatService = chatService;

            // Load current username from config (editable)
            txtUsername.Text = _chatService.ConfigManager.Config.Username;
            txtUsername.ReadOnly = false;
            txtUsername.Enabled = true;
            txtUsername.MaxLength = 30;

            txtDownloadPath.Text = _chatService.ConfigManager.Config.DownloadFolder;
            txtServerIp.Text = _chatService.ConfigManager.Config.ServerIp;
            nudServerPort.Value = Math.Clamp(_chatService.ConfigManager.Config.ServerPort, 1, 65535);
            chkEnableSound.Checked = _chatService.ConfigManager.Config.EnableSound;
            chkStartWithWindows.Checked = _chatService.ConfigManager.Config.StartWithWindows;
            _selectedAvatarIndex = _chatService.ConfigManager.Config.AvatarIndex;

            InitializeAvatarButtons();
            SelectAvatar(_selectedAvatarIndex);
        }

        private void InitializeAvatarButtons()
        {
            Color[] avatarColors = {
                Color.FromArgb(88, 101, 242),
                Color.FromArgb(237, 66, 69),
                Color.FromArgb(240, 167, 4),
                Color.FromArgb(35, 165, 90),
                Color.FromArgb(155, 89, 182),
                Color.FromArgb(233, 30, 99),
                Color.FromArgb(26, 188, 156),
                Color.FromArgb(52, 152, 219)
            };

            for (int i = 0; i < 8; i++)
            {
                var btn = new Button
                {
                    Width = ScaleForDpi(40),
                    Height = ScaleForDpi(40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = avatarColors[i],
                    Tag = i,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += AvatarButton_Click;

                flpAvatars.Controls.Add(btn);
                _avatarButtons[i] = btn;
            }
        }

        private void AvatarButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int index)
            {
                SelectAvatar(index);
            }
        }

        private void SelectAvatar(int index)
        {
            _selectedAvatarIndex = index;
            for (int i = 0; i < 8; i++)
            {
                if (i == index)
                {
                    _avatarButtons[i].FlatAppearance.BorderSize = 3;
                    _avatarButtons[i].FlatAppearance.BorderColor = Color.White;
                }
                else
                {
                    _avatarButtons[i].FlatAppearance.BorderSize = 0;
                }
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (Directory.Exists(txtDownloadPath.Text))
                {
                    fbd.SelectedPath = txtDownloadPath.Text;
                }

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDownloadPath.Text = fbd.SelectedPath;
                }
            }
        }

        private async void btnSave_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Tên hiển thị không được để trống!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (username.Length > 30)
            {
                MessageBox.Show("Tên hiển thị tối đa 30 ký tự!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string downloadFolder = txtDownloadPath.Text.Trim();
            string serverIp = txtServerIp.Text.Trim();
            if (string.IsNullOrWhiteSpace(downloadFolder))
            {
                MessageBox.Show("Downloads folder cannot be empty!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(serverIp))
            {
                MessageBox.Show("Server host cannot be empty. Use the LAN IP of the machine running LANChatServer.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(downloadFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Cannot access downloads folder: {ex.Message}", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool networkChanged =
                !string.Equals(_chatService.ConfigManager.Config.ServerIp, serverIp, StringComparison.OrdinalIgnoreCase) ||
                _chatService.ConfigManager.Config.ServerPort != (int)nudServerPort.Value;

            btnSave.Enabled = false;
            UseWaitCursor = true;

            try
            {
                if (networkChanged)
                {
                    bool serverAvailable = await ChatService.CanConnectToServerAsync(serverIp, (int)nudServerPort.Value);
                    if (!serverAvailable)
                    {
                        MessageBox.Show(
                            $"Cannot connect to LANChatServer at {serverIp}:{(int)nudServerPort.Value}.{Environment.NewLine}Start the server before using this address.",
                            "Server unavailable",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }
            }
            finally
            {
                UseWaitCursor = false;
                btnSave.Enabled = true;
            }

            string oldServerIp = _chatService.ConfigManager.Config.ServerIp;
            int oldServerPort = _chatService.ConfigManager.Config.ServerPort;

            _chatService.ConfigManager.Config.Username = username;
            _chatService.ConfigManager.Config.DownloadFolder = downloadFolder;
            _chatService.ConfigManager.Config.ServerIp = serverIp;
            _chatService.ConfigManager.Config.ServerPort = (int)nudServerPort.Value;
            _chatService.ConfigManager.Config.EnableSound = chkEnableSound.Checked;
            _chatService.ConfigManager.Config.StartWithWindows = chkStartWithWindows.Checked;
            _chatService.ConfigManager.Config.AvatarIndex = _selectedAvatarIndex;

            if (networkChanged)
            {
                btnSave.Enabled = false;
                UseWaitCursor = true;
                bool restarted = await _chatService.RestartNetworkAsync();
                UseWaitCursor = false;
                btnSave.Enabled = true;

                if (!restarted)
                {
                    _chatService.ConfigManager.Config.ServerIp = oldServerIp;
                    _chatService.ConfigManager.Config.ServerPort = oldServerPort;
                    await _chatService.RestartNetworkAsync();

                    MessageBox.Show(
                        "Cannot reconnect with the new server settings. The previous server settings were restored.",
                        "Server reconnect failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
            }

            _chatService.ConfigManager.Save();

            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private int ScaleForDpi(int value)
        {
            return (int)Math.Round(value * (DeviceDpi / 96.0));
        }
    }
}
