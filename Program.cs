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
            Application.Run(new MainForm());
        }
    }
}
