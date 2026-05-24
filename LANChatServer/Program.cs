using System;
using System.Threading;
using System.Windows.Forms;

namespace LANChatServer
{
    static class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        static void Main()
        {
            const string mutexName = "Global\\LANChatProServerUniqueMutexName";
            _mutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                MessageBox.Show("LANChat Server hiện đang chạy! Bạn chỉ có thể mở duy nhất một máy chủ tại một thời điểm.", 
                                "Cảnh báo", 
                                MessageBoxButtons.OK, 
                                MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Register COM Marshalling for Native AOT WinForms stability
                System.Runtime.InteropServices.ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);

                ApplicationConfiguration.Initialize();
                Application.Run(new ServerForm());
            }
            finally
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }
    }
}
