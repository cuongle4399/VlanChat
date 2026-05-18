namespace LANChatPro.Forms
{
    partial class PeerDetailsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.PictureBox picAvatar;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblMachineLabel;
        private System.Windows.Forms.Label lblMachineName;
        private System.Windows.Forms.Label lblIpLabel;
        private System.Windows.Forms.Label lblIpAddress;
        private System.Windows.Forms.Label lblPortLabel;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.Label lblStatusLabel;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblLastSeenLabel;
        private System.Windows.Forms.Label lblLastSeen;
        private System.Windows.Forms.Button btnPing;
        private System.Windows.Forms.Label lblPingResult;
        private System.Windows.Forms.Button btnMessage;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Panel pnlCard;

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
            this.picAvatar = new System.Windows.Forms.PictureBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblMachineLabel = new System.Windows.Forms.Label();
            this.lblMachineName = new System.Windows.Forms.Label();
            this.lblIpLabel = new System.Windows.Forms.Label();
            this.lblIpAddress = new System.Windows.Forms.Label();
            this.lblPortLabel = new System.Windows.Forms.Label();
            this.lblPort = new System.Windows.Forms.Label();
            this.lblStatusLabel = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblLastSeenLabel = new System.Windows.Forms.Label();
            this.lblLastSeen = new System.Windows.Forms.Label();
            this.btnPing = new System.Windows.Forms.Button();
            this.lblPingResult = new System.Windows.Forms.Label();
            this.btnMessage = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlCard = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.picAvatar)).BeginInit();
            this.pnlCard.SuspendLayout();
            this.SuspendLayout();
            // 
            // picAvatar
            // 
            this.picAvatar.Location = new System.Drawing.Point(185, 20);
            this.picAvatar.Name = "picAvatar";
            this.picAvatar.Size = new System.Drawing.Size(80, 80);
            this.picAvatar.TabIndex = 0;
            this.picAvatar.TabStop = false;
            // 
            // lblUsername
            // 
            this.lblUsername.AutoEllipsis = true;
            this.lblUsername.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblUsername.ForeColor = System.Drawing.Color.White;
            this.lblUsername.Location = new System.Drawing.Point(20, 110);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(410, 30);
            this.lblUsername.TabIndex = 1;
            this.lblUsername.Text = "Username";
            this.lblUsername.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlCard
            // 
            this.pnlCard.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(45)))), ((int)(((byte)(49)))));
            this.pnlCard.Controls.Add(this.lblMachineLabel);
            this.pnlCard.Controls.Add(this.lblMachineName);
            this.pnlCard.Controls.Add(this.lblIpLabel);
            this.pnlCard.Controls.Add(this.lblIpAddress);
            this.pnlCard.Controls.Add(this.lblPortLabel);
            this.pnlCard.Controls.Add(this.lblPort);
            this.pnlCard.Controls.Add(this.lblStatusLabel);
            this.pnlCard.Controls.Add(this.lblStatus);
            this.pnlCard.Controls.Add(this.lblLastSeenLabel);
            this.pnlCard.Controls.Add(this.lblLastSeen);
            this.pnlCard.Location = new System.Drawing.Point(25, 160);
            this.pnlCard.Name = "pnlCard";
            this.pnlCard.Size = new System.Drawing.Size(400, 175);
            this.pnlCard.TabIndex = 2;
            // 
            // lblMachineLabel
            // 
            this.lblMachineLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblMachineLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(155)))), ((int)(((byte)(164)))));
            this.lblMachineLabel.Location = new System.Drawing.Point(15, 12);
            this.lblMachineLabel.Name = "lblMachineLabel";
            this.lblMachineLabel.Size = new System.Drawing.Size(120, 20);
            this.lblMachineLabel.TabIndex = 0;
            this.lblMachineLabel.Text = "Tên máy tính:";
            this.lblMachineLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblMachineName
            // 
            this.lblMachineName.AutoEllipsis = true;
            this.lblMachineName.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblMachineName.ForeColor = System.Drawing.Color.White;
            this.lblMachineName.Location = new System.Drawing.Point(140, 12);
            this.lblMachineName.Name = "lblMachineName";
            this.lblMachineName.Size = new System.Drawing.Size(245, 20);
            this.lblMachineName.TabIndex = 1;
            this.lblMachineName.Text = "N/A";
            this.lblMachineName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblIpLabel
            // 
            this.lblIpLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblIpLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(155)))), ((int)(((byte)(164)))));
            this.lblIpLabel.Location = new System.Drawing.Point(15, 42);
            this.lblIpLabel.Name = "lblIpLabel";
            this.lblIpLabel.Size = new System.Drawing.Size(120, 20);
            this.lblIpLabel.TabIndex = 2;
            this.lblIpLabel.Text = "Địa chỉ IP LAN:";
            this.lblIpLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblIpAddress
            // 
            this.lblIpAddress.AutoEllipsis = true;
            this.lblIpAddress.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblIpAddress.ForeColor = System.Drawing.Color.White;
            this.lblIpAddress.Location = new System.Drawing.Point(140, 42);
            this.lblIpAddress.Name = "lblIpAddress";
            this.lblIpAddress.Size = new System.Drawing.Size(245, 20);
            this.lblIpAddress.TabIndex = 3;
            this.lblIpAddress.Text = "0.0.0.0";
            this.lblIpAddress.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPortLabel
            // 
            this.lblPortLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblPortLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(155)))), ((int)(((byte)(164)))));
            this.lblPortLabel.Location = new System.Drawing.Point(15, 72);
            this.lblPortLabel.Name = "lblPortLabel";
            this.lblPortLabel.Size = new System.Drawing.Size(120, 20);
            this.lblPortLabel.TabIndex = 4;
            this.lblPortLabel.Text = "Cổng TCP Chat:";
            this.lblPortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblPort
            // 
            this.lblPort.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblPort.ForeColor = System.Drawing.Color.White;
            this.lblPort.Location = new System.Drawing.Point(140, 72);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(245, 20);
            this.lblPort.TabIndex = 5;
            this.lblPort.Text = "50002";
            this.lblPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStatusLabel
            // 
            this.lblStatusLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(155)))), ((int)(((byte)(164)))));
            this.lblStatusLabel.Location = new System.Drawing.Point(15, 102);
            this.lblStatusLabel.Name = "lblStatusLabel";
            this.lblStatusLabel.Size = new System.Drawing.Size(120, 20);
            this.lblStatusLabel.TabIndex = 6;
            this.lblStatusLabel.Text = "Trạng thái:";
            this.lblStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblStatus.ForeColor = System.Drawing.Color.White;
            this.lblStatus.Location = new System.Drawing.Point(140, 102);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(245, 20);
            this.lblStatus.TabIndex = 7;
            this.lblStatus.Text = "N/A";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLastSeenLabel
            // 
            this.lblLastSeenLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblLastSeenLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(155)))), ((int)(((byte)(164)))));
            this.lblLastSeenLabel.Location = new System.Drawing.Point(15, 132);
            this.lblLastSeenLabel.Name = "lblLastSeenLabel";
            this.lblLastSeenLabel.Size = new System.Drawing.Size(120, 20);
            this.lblLastSeenLabel.TabIndex = 8;
            this.lblLastSeenLabel.Text = "Tương tác cuối:";
            this.lblLastSeenLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLastSeen
            // 
            this.lblLastSeen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblLastSeen.ForeColor = System.Drawing.Color.White;
            this.lblLastSeen.Location = new System.Drawing.Point(140, 132);
            this.lblLastSeen.Name = "lblLastSeen";
            this.lblLastSeen.Size = new System.Drawing.Size(245, 20);
            this.lblLastSeen.TabIndex = 9;
            this.lblLastSeen.Text = "N/A";
            this.lblLastSeen.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnPing
            // 
            this.btnPing.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(101)))), ((int)(((byte)(242)))));
            this.btnPing.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPing.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnPing.ForeColor = System.Drawing.Color.White;
            this.btnPing.Location = new System.Drawing.Point(25, 350);
            this.btnPing.Name = "btnPing";
            this.btnPing.Size = new System.Drawing.Size(120, 30);
            this.btnPing.TabIndex = 3;
            this.btnPing.Text = "Kiểm tra Ping";
            this.btnPing.UseVisualStyleBackColor = false;
            this.btnPing.Click += new System.EventHandler(this.btnPing_Click);
            // 
            // lblPingResult
            // 
            this.lblPingResult.AutoEllipsis = true;
            this.lblPingResult.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.lblPingResult.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(148)))), ((int)(((byte)(155)))), ((int)(((byte)(164)))));
            this.lblPingResult.Location = new System.Drawing.Point(155, 350);
            this.lblPingResult.Name = "lblPingResult";
            this.lblPingResult.Size = new System.Drawing.Size(270, 30);
            this.lblPingResult.TabIndex = 4;
            this.lblPingResult.Text = "Chưa kiểm tra.";
            this.lblPingResult.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnMessage
            // 
            this.btnMessage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(165)))), ((int)(((byte)(90)))));
            this.btnMessage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMessage.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnMessage.ForeColor = System.Drawing.Color.White;
            this.btnMessage.Location = new System.Drawing.Point(85, 410);
            this.btnMessage.Name = "btnMessage";
            this.btnMessage.Size = new System.Drawing.Size(130, 35);
            this.btnMessage.TabIndex = 5;
            this.btnMessage.Text = "Nhắn tin";
            this.btnMessage.UseVisualStyleBackColor = false;
            this.btnMessage.Click += new System.EventHandler(this.btnMessage_Click);
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(78)))), ((int)(((byte)(80)))), ((int)(((byte)(88)))));
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(235, 410);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(130, 35);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "Đóng";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // PeerDetailsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(31)))), ((int)(((byte)(34)))));
            this.ClientSize = new System.Drawing.Size(450, 470);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnMessage);
            this.Controls.Add(this.lblPingResult);
            this.Controls.Add(this.btnPing);
            this.Controls.Add(this.pnlCard);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.picAvatar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(466, 509);
            this.Name = "PeerDetailsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Thông tin chi tiết";
            ((System.ComponentModel.ISupportInitialize)(this.picAvatar)).EndInit();
            this.pnlCard.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
