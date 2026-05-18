namespace LANChatPro.Forms
{
    partial class ToastForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Panel pnlAccent;
        private System.Windows.Forms.PictureBox picAvatar;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblMessage;

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
            this.pnlAccent = new System.Windows.Forms.Panel();
            this.picAvatar = new System.Windows.Forms.PictureBox();
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblMessage = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picAvatar)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlAccent
            // 
            this.pnlAccent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(88)))), ((int)(((byte)(101)))), ((int)(((byte)(242)))));
            this.pnlAccent.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlAccent.Location = new System.Drawing.Point(0, 0);
            this.pnlAccent.Name = "pnlAccent";
            this.pnlAccent.Size = new System.Drawing.Size(6, 80);
            this.pnlAccent.TabIndex = 0;
            // 
            // picAvatar
            // 
            this.picAvatar.Location = new System.Drawing.Point(18, 16);
            this.picAvatar.Name = "picAvatar";
            this.picAvatar.Size = new System.Drawing.Size(48, 48);
            this.picAvatar.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picAvatar.TabIndex = 1;
            this.picAvatar.TabStop = false;
            // 
            // lblTitle
            // 
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(75, 14);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(233, 18);
            this.lblTitle.TabIndex = 2;
            this.lblTitle.Text = "Notification Title";
            // 
            // lblMessage
            // 
            this.lblMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMessage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(219)))), ((int)(((byte)(222)))), ((int)(((byte)(225)))));
            this.lblMessage.Location = new System.Drawing.Point(75, 36);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(233, 35);
            this.lblMessage.TabIndex = 3;
            this.lblMessage.Text = "Notification body message goes here and is wrapped beautifully.";
            // 
            // ToastForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(45)))), ((int)(((byte)(49)))));
            this.ClientSize = new System.Drawing.Size(320, 80);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.picAvatar);
            this.Controls.Add(this.pnlAccent);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ToastForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "ToastForm";
            ((System.ComponentModel.ISupportInitialize)(this.picAvatar)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
