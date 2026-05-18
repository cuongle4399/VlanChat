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

            // Load config values into controls, enforcing machine name as read-only identity
            txtUsername.Text = Environment.MachineName;
            txtUsername.ReadOnly = true;
            txtUsername.Enabled = false;

            txtDownloadPath.Text = _chatService.ConfigManager.Config.DownloadFolder;
            chkEnableSound.Checked = _chatService.ConfigManager.Config.EnableSound;
            chkStartWithWindows.Checked = _chatService.ConfigManager.Config.StartWithWindows;
            _selectedAvatarIndex = _chatService.ConfigManager.Config.AvatarIndex;

            InitializeAvatarButtons();
            SelectAvatar(_selectedAvatarIndex);
        }

        private void InitializeAvatarButtons()
        {
            Color[] avatarColors = {
                Color.FromArgb(88, 101, 242),  // Discord Blurple
                Color.FromArgb(237, 66, 69),   // Crimson Red
                Color.FromArgb(240, 167, 4),    // Sun Golden Yellow
                Color.FromArgb(35, 165, 90),   // Emerald Green
                Color.FromArgb(155, 89, 182),  // Amethyst Purple
                Color.FromArgb(233, 30, 99),   // Deep Hot Pink
                Color.FromArgb(26, 188, 156),  // Teal Turquoise
                Color.FromArgb(52, 152, 219)   // Sky Blue
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

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Username cannot be empty!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string downloadFolder = txtDownloadPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(downloadFolder))
            {
                MessageBox.Show("Downloads folder cannot be empty!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            _chatService.ConfigManager.Config.Username = txtUsername.Text.Trim();
            _chatService.ConfigManager.Config.DownloadFolder = downloadFolder;
            _chatService.ConfigManager.Config.EnableSound = chkEnableSound.Checked;
            _chatService.ConfigManager.Config.StartWithWindows = chkStartWithWindows.Checked;
            _chatService.ConfigManager.Config.AvatarIndex = _selectedAvatarIndex;

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
