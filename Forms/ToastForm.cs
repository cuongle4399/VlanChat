using System;
using System.Drawing;
using System.Windows.Forms;

namespace LANChatPro.Forms
{
    public partial class ToastForm : Form
    {
        private readonly System.Windows.Forms.Timer _timer = new();
        private int _step = 0;
        private int _displayCount = 0;
        private readonly int _targetX;
        private readonly int _targetY;
        private int _currentY;

        public ToastForm(string title, string message, Image? avatar = null)
        {
            InitializeComponent();

            lblTitle.Text = title;
            lblMessage.Text = message;

            if (avatar != null)
            {
                picAvatar.Image = avatar;
            }
            else
            {
                picAvatar.Image = CreateDefaultAvatar(title);
            }

this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Width = ScaleForDpi(320);
            this.Height = ScaleForDpi(80);
            this.BackColor = Color.FromArgb(43, 45, 49);

Rectangle workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
            _targetX = workingArea.Right - this.Width - 15;
            _targetY = workingArea.Bottom - this.Height - 15;
            _currentY = workingArea.Bottom;

            this.Location = new Point(_targetX, _currentY);
            this.Opacity = 0;

            _timer.Interval = 15;
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer.Stop();
            _timer.Dispose();
            base.OnFormClosed(e);
        }

        private int ScaleForDpi(int value)
        {
            return (int)Math.Round(value * (DeviceDpi / 96.0));
        }

        private Image CreateDefaultAvatar(string text)
        {
            Bitmap bmp = new Bitmap(48, 48);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                char letter = string.IsNullOrEmpty(text) ? 'U' : text[0];

using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, 48, 48),
                    Color.FromArgb(88, 101, 242),
                    Color.FromArgb(114, 137, 218),
                    45f))
                {
                    g.FillEllipse(brush, 0, 0, 48, 48);
                }

using (Font font = new Font("Segoe UI", 16, FontStyle.Bold))
                using (Brush brush = new SolidBrush(Color.White))
                {
                    string letterStr = letter.ToString().ToUpper();
                    SizeF size = g.MeasureString(letterStr, font);
                    g.DrawString(letterStr, font, brush, (48 - size.Width) / 2, (48 - size.Height) / 2);
                }
            }
            return bmp;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_step == 0)
            {
                _currentY -= 6;
                this.Opacity += 0.08;

                if (_currentY <= _targetY)
                {
                    _currentY = _targetY;
                    this.Opacity = 1.0;
                    _step = 1;
                }
                this.Location = new Point(_targetX, _currentY);
            }
            else if (_step == 1)
            {
                _displayCount++;
                if (_displayCount >= 180)
                {
                    _step = 2;
                }
            }
            else if (_step == 2)
            {
                this.Opacity -= 0.05;
                _currentY += 2;
                this.Location = new Point(_targetX, _currentY);

                if (this.Opacity <= 0)
                {
                    _timer.Stop();
                    this.Close();
                }
            }
        }
    }
}
