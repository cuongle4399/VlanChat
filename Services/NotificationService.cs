using System;
using System.Drawing;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using LANChatPro.Storage;

namespace LANChatPro.Services
{
    public class NotificationService
    {
        private readonly ConfigManager _configManager;
        private readonly SynchronizationContext? _uiContext;

        public NotificationService(ConfigManager configManager)
        {
            _configManager = configManager;
            _uiContext = SynchronizationContext.Current;
        }

        public void PlayMessageSound()
        {
            if (_configManager.Config.EnableSound)
            {
                try
                {
                    SystemSounds.Asterisk.Play();
                }
                catch
                {
                    // Suppress audio failure
                }
            }
        }

        public void PlayOnlineSound()
        {
            if (_configManager.Config.EnableSound)
            {
                try
                {
                    SystemSounds.Beep.Play();
                }
                catch
                {
                    // Suppress audio failure
                }
            }
        }

        public void ShowToast(string title, string message, Image? avatar = null)
        {
            try
            {
                if (_uiContext != null)
                {
                    _uiContext.Post(_ => ShowToastInternal(title, message, avatar), null);
                    return;
                }

                if (Application.OpenForms.Count > 0)
                {
                    Form? activeForm = Application.OpenForms[0];
                    if (activeForm != null && activeForm.InvokeRequired)
                    {
                        activeForm.BeginInvoke(new Action(() => ShowToastInternal(title, message, avatar)));
                    }
                    else
                    {
                        ShowToastInternal(title, message, avatar);
                    }
                }
                else
                {
                    // Fallback to thread-safe execution if no active forms yet
                    ShowToastInternal(title, message, avatar);
                }
            }
            catch
            {
                // Suppress toast errors
            }
        }

        private void ShowToastInternal(string title, string message, Image? avatar)
        {
            try
            {
                var toast = new Forms.ToastForm(title, message, avatar);
                toast.Show();
            }
            catch
            {
                // Suppress
            }
        }
    }
}
