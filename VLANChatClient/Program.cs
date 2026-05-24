using System;
using System.Windows.Forms;
using LANChatPro.Forms;
using LANChatPro.Utils;

namespace LANChatPro
{
    static class Program
    {

[STAThread]
        static void Main()
        {

            System.Runtime.InteropServices.ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);

            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                Logger.Error("Unhandled UI exception", e.Exception);
                MessageBox.Show(e.Exception.Message, "LAN Chat Pro Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Logger.Error("Unhandled application exception", ex);
                }
            };

            // Loop: ConnectionCheckForm → MainForm → reconnect → ConnectionCheckForm...
            bool reconnect = true;
            while (reconnect)
            {
                reconnect = false;
                using (var checkForm = new ConnectionCheckForm())
                {
                    if (checkForm.ShowDialog() != DialogResult.OK)
                        break; // User closed / exit

                    using (var mainForm = new MainForm())
                    {
                        Application.Run(mainForm);
                        // MainForm sets Tag = "reconnect" when server disconnects
                        reconnect = mainForm.Tag is string tag && tag == "reconnect";
                    }
                }
            }
        }
    }
}
