namespace LANChatPro.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel pnlSidebar;
        private System.Windows.Forms.Panel pnlChannelsHeader;
        private System.Windows.Forms.Label lblChannelsHeader;
        private System.Windows.Forms.ListBox lstChannels;
        private System.Windows.Forms.Label lblOnlineHeader;
        private System.Windows.Forms.ListBox lstPeers;
        private System.Windows.Forms.Panel pnlMyProfile;
        private System.Windows.Forms.PictureBox picMyAvatar;
        private System.Windows.Forms.Label lblMyName;
        private System.Windows.Forms.Button btnSettings;
        
        private System.Windows.Forms.Panel pnlChatArea;
        private System.Windows.Forms.Panel pnlChatHeader;
        private System.Windows.Forms.Label lblHeaderTitle;
        private System.Windows.Forms.Label lblHeaderDetails;
        private System.Windows.Forms.RichTextBox rtbChat;
        private System.Windows.Forms.Panel pnlChatInput;
        private System.Windows.Forms.Button btnEmoji;
        private System.Windows.Forms.TextBox txtMessage;
        private System.Windows.Forms.Button btnSend;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlSidebar = new System.Windows.Forms.Panel();
            this.pnlChannelsHeader = new System.Windows.Forms.Panel();
            this.lblChannelsHeader = new System.Windows.Forms.Label();
            this.lstChannels = new System.Windows.Forms.ListBox();
            this.lblOnlineHeader = new System.Windows.Forms.Label();
            this.lstPeers = new System.Windows.Forms.ListBox();
            this.pnlMyProfile = new System.Windows.Forms.Panel();
            this.picMyAvatar = new System.Windows.Forms.PictureBox();
            this.lblMyName = new System.Windows.Forms.Label();
            this.btnSettings = new System.Windows.Forms.Button();
            this.pnlChatArea = new System.Windows.Forms.Panel();
            this.pnlChatHeader = new System.Windows.Forms.Panel();
            this.lblHeaderTitle = new System.Windows.Forms.Label();
            this.lblHeaderDetails = new System.Windows.Forms.Label();
            this.rtbChat = new System.Windows.Forms.RichTextBox();
            this.pnlChatInput = new System.Windows.Forms.Panel();
            this.btnEmoji = new System.Windows.Forms.Button();
            this.txtMessage = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.pnlSidebar.SuspendLayout();
            this.pnlChannelsHeader.SuspendLayout();
            this.pnlMyProfile.SuspendLayout();
            this.pnlChatArea.SuspendLayout();
            this.pnlChatHeader.SuspendLayout();
            this.pnlChatInput.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMyAvatar)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlSidebar
            // 
            this.pnlSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(45)))), ((int)(((byte)(49)))));
            this.pnlSidebar.Controls.Add(this.lstPeers);
            this.pnlSidebar.Controls.Add(this.lblOnlineHeader);
            this.pnlSidebar.Controls.Add(this.lstChannels);
            this.pnlSidebar.Controls.Add(this.pnlChannelsHeader);
            this.pnlSidebar.Controls.Add(this.pnlMyProfile);
            this.pnlSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSidebar.Location = new System.Drawing.Point(0, 0);
            this.pnlSidebar.Name = "pnlSidebar";
            this.pnlSidebar.Size = new System.Drawing.Size(250, 550);
            this.pnlSidebar.TabIndex = 0;
            // 
            // pnlChannelsHeader
            // 
            this.pnlChannelsHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(34)))));
            this.pnlChannelsHeader.Controls.Add(this.lblChannelsHeader);
            this.pnlChannelsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlChannelsHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlChannelsHeader.Name = "pnlChannelsHeader";
            this.pnlChannelsHeader.Size = new System.Drawing.Size(250, 55);
            this.pnlChannelsHeader.TabIndex = 0;
            // 
            // lblChannelsHeader
            // 
            this.lblChannelsHeader.AutoSize = true;
            this.lblChannelsHeader.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblChannelsHeader.ForeColor = System.Drawing.Color.White;
            this.lblChannelsHeader.Location = new System.Drawing.Point(15, 18);
            this.lblChannelsHeader.Name = "lblChannelsHeader";
            this.lblChannelsHeader.Size = new System.Drawing.Size(139, 17);
            this.lblChannelsHeader.TabIndex = 0;
            this.lblChannelsHeader.Text = "KÊNH LAN CHAT PRO";
            // 
            // lstChannels
            // 
            this.lstChannels.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(45)))), ((int)(((byte)(49)))));
            this.lstChannels.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstChannels.Dock = System.Windows.Forms.DockStyle.Top;
            this.lstChannels.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lstChannels.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(219)))), ((int)(((byte)(222)))), ((int)(((byte)(225)))));
            this.lstChannels.FormattingEnabled = true;
            this.lstChannels.ItemHeight = 17;
            this.lstChannels.Items.AddRange(new object[] {
            "  # Kênh chung"});
            this.lstChannels.Location = new System.Drawing.Point(0, 55);
            this.lstChannels.Name = "lstChannels";
            this.lstChannels.Size = new System.Drawing.Size(250, 34);
            this.lstChannels.TabIndex = 1;
            // 
            // lblOnlineHeader
            // 
            this.lblOnlineHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(45)))), ((int)(((byte)(49)))));
            this.lblOnlineHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblOnlineHeader.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblOnlineHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(155)))), ((int)(((byte)(164)))));
            this.lblOnlineHeader.Location = new System.Drawing.Point(0, 89);
            this.lblOnlineHeader.Name = "lblOnlineHeader";
            this.lblOnlineHeader.Size = new System.Drawing.Size(250, 25);
            this.lblOnlineHeader.TabIndex = 2;
            this.lblOnlineHeader.Text = "  NGƯỜI ONLINE";
            this.lblOnlineHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lstPeers
            // 
            this.lstPeers.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(45)))), ((int)(((byte)(49)))));
            this.lstPeers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lstPeers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstPeers.ForeColor = System.Drawing.Color.White;
            this.lstPeers.FormattingEnabled = true;
            this.lstPeers.ItemHeight = 15;
            this.lstPeers.Location = new System.Drawing.Point(0, 114);
            this.lstPeers.Name = "lstPeers";
            this.lstPeers.Size = new System.Drawing.Size(250, 381);
            this.lstPeers.TabIndex = 3;
            this.lstPeers.DoubleClick += new System.EventHandler(this.lstPeers_DoubleClick);
            this.lstPeers.MouseDown += new System.Windows.Forms.MouseEventHandler(this.lstPeers_MouseDown);
            // 
            // pnlMyProfile
            // 
            this.pnlMyProfile.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(36)))), ((int)(((byte)(40)))));
            this.pnlMyProfile.Controls.Add(this.btnSettings);
            this.pnlMyProfile.Controls.Add(this.lblMyName);
            this.pnlMyProfile.Controls.Add(this.picMyAvatar);
            this.pnlMyProfile.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlMyProfile.Location = new System.Drawing.Point(0, 495);
            this.pnlMyProfile.Name = "pnlMyProfile";
            this.pnlMyProfile.Size = new System.Drawing.Size(250, 55);
            this.pnlMyProfile.TabIndex = 4;
            // 
            // picMyAvatar
            // 
            this.picMyAvatar.Location = new System.Drawing.Point(12, 10);
            this.picMyAvatar.Name = "picMyAvatar";
            this.picMyAvatar.Size = new System.Drawing.Size(36, 36);
            this.picMyAvatar.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picMyAvatar.TabIndex = 0;
            this.picMyAvatar.TabStop = false;
            // 
            // lblMyName
            // 
            this.lblMyName.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblMyName.ForeColor = System.Drawing.Color.White;
            this.lblMyName.Location = new System.Drawing.Point(56, 18);
            this.lblMyName.Name = "lblMyName";
            this.lblMyName.Size = new System.Drawing.Size(130, 20);
            this.lblMyName.TabIndex = 1;
            this.lblMyName.Text = "Username";
            // 
            // btnSettings
            // 
            this.btnSettings.BackColor = System.Drawing.Color.Transparent;
            this.btnSettings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSettings.FlatAppearance.BorderSize = 0;
            this.btnSettings.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btnSettings.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(45)))), ((int)(((byte)(49)))));
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(181)))), ((int)(((byte)(186)))), ((int)(((byte)(193)))));
            this.btnSettings.Location = new System.Drawing.Point(205, 12);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(32, 32);
            this.btnSettings.TabIndex = 2;
            this.btnSettings.Text = "⚙";
            this.btnSettings.UseVisualStyleBackColor = false;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // pnlChatArea
            // 
            this.pnlChatArea.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.pnlChatArea.Controls.Add(this.rtbChat);
            this.pnlChatArea.Controls.Add(this.pnlChatInput);
            this.pnlChatArea.Controls.Add(this.pnlChatHeader);
            this.pnlChatArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlChatArea.Location = new System.Drawing.Point(250, 0);
            this.pnlChatArea.Name = "pnlChatArea";
            this.pnlChatArea.Size = new System.Drawing.Size(600, 550);
            this.pnlChatArea.TabIndex = 1;
            // 
            // pnlChatHeader
            // 
            this.pnlChatHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.pnlChatHeader.Controls.Add(this.lblHeaderDetails);
            this.pnlChatHeader.Controls.Add(this.lblHeaderTitle);
            this.pnlChatHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlChatHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlChatHeader.Name = "pnlChatHeader";
            this.pnlChatHeader.Size = new System.Drawing.Size(600, 55);
            this.pnlChatHeader.TabIndex = 0;
            // 
            // lblHeaderTitle
            // 
            this.lblHeaderTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHeaderTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblHeaderTitle.ForeColor = System.Drawing.Color.White;
            this.lblHeaderTitle.Location = new System.Drawing.Point(20, 10);
            this.lblHeaderTitle.Name = "lblHeaderTitle";
            this.lblHeaderTitle.Size = new System.Drawing.Size(560, 21);
            this.lblHeaderTitle.TabIndex = 0;
            this.lblHeaderTitle.Text = "# Kênh chung";
            // 
            // lblHeaderDetails
            // 
            this.lblHeaderDetails.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHeaderDetails.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblHeaderDetails.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(155)))), ((int)(((byte)(164)))));
            this.lblHeaderDetails.Location = new System.Drawing.Point(20, 31);
            this.lblHeaderDetails.Name = "lblHeaderDetails";
            this.lblHeaderDetails.Size = new System.Drawing.Size(560, 15);
            this.lblHeaderDetails.TabIndex = 1;
            this.lblHeaderDetails.Text = "My Node: 127.0.0.1:50002";
            // 
            // rtbChat
            // 
            this.rtbChat.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.rtbChat.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbChat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbChat.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.rtbChat.ForeColor = System.Drawing.Color.White;
            this.rtbChat.Location = new System.Drawing.Point(0, 55);
            this.rtbChat.Name = "rtbChat";
            this.rtbChat.ReadOnly = true;
            this.rtbChat.Size = new System.Drawing.Size(600, 435);
            this.rtbChat.TabIndex = 1;
            this.rtbChat.Text = "";
            // 
            // pnlChatInput
            // 
            this.pnlChatInput.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(45)))), ((int)(((byte)(49)))));
            this.pnlChatInput.Controls.Add(this.btnSend);
            this.pnlChatInput.Controls.Add(this.txtMessage);
            this.pnlChatInput.Controls.Add(this.btnEmoji);
            this.pnlChatInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlChatInput.Location = new System.Drawing.Point(0, 490);
            this.pnlChatInput.Name = "pnlChatInput";
            this.pnlChatInput.Size = new System.Drawing.Size(600, 60);
            this.pnlChatInput.TabIndex = 2;
            // 
            // btnEmoji
            // 
            this.btnEmoji.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.btnEmoji.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEmoji.FlatAppearance.BorderSize = 0;
            this.btnEmoji.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEmoji.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnEmoji.ForeColor = System.Drawing.Color.White;
            this.btnEmoji.Location = new System.Drawing.Point(15, 15);
            this.btnEmoji.Name = "btnEmoji";
            this.btnEmoji.Size = new System.Drawing.Size(35, 30);
            this.btnEmoji.TabIndex = 0;
            this.btnEmoji.Text = "😀";
            this.btnEmoji.UseVisualStyleBackColor = false;
            this.btnEmoji.Click += new System.EventHandler(this.btnEmoji_Click);
            // 
            // txtMessage
            // 
            this.txtMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMessage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(58)))), ((int)(((byte)(64)))));
            this.txtMessage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtMessage.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtMessage.ForeColor = System.Drawing.Color.White;
            this.txtMessage.Location = new System.Drawing.Point(60, 16);
            this.txtMessage.Name = "txtMessage";
            this.txtMessage.Size = new System.Drawing.Size(445, 25);
            this.txtMessage.TabIndex = 1;
            this.txtMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtMessage_KeyDown);
            // 
            // btnSend
            // 
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(101)))), ((int)(((byte)(242)))));
            this.btnSend.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSend.FlatAppearance.BorderSize = 0;
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnSend.ForeColor = System.Drawing.Color.White;
            this.btnSend.Location = new System.Drawing.Point(520, 14);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(65, 30);
            this.btnSend.TabIndex = 2;
            this.btnSend.Text = "Send";
            this.btnSend.UseVisualStyleBackColor = false;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(49)))), ((int)(((byte)(51)))), ((int)(((byte)(56)))));
            this.ClientSize = new System.Drawing.Size(850, 550);
            this.Controls.Add(this.pnlChatArea);
            this.Controls.Add(this.pnlSidebar);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.MinimumSize = new System.Drawing.Size(760, 480);
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LAN Chat Pro";
            this.pnlSidebar.ResumeLayout(false);
            this.pnlChannelsHeader.ResumeLayout(false);
            this.pnlChannelsHeader.PerformLayout();
            this.pnlMyProfile.ResumeLayout(false);
            this.pnlChatArea.ResumeLayout(false);
            this.pnlChatHeader.ResumeLayout(false);
            this.pnlChatHeader.PerformLayout();
            this.pnlChatInput.ResumeLayout(false);
            this.pnlChatInput.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMyAvatar)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
