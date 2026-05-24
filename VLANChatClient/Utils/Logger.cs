using System;
using System.IO;

namespace LANChatPro.Utils
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LANChatPro",
            "app.log"
        );

        private static readonly object LockObj = new object();

        public static void Log(string message, string level = "INFO")
        {
            try
            {
                string dir = Path.GetDirectoryName(LogPath) ?? string.Empty;
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                System.Diagnostics.Debug.WriteLine(logLine);

                lock (LockObj)
                {
                    File.AppendAllText(LogPath, logLine + Environment.NewLine);
                }
            }
            catch
            {

            }
        }

        public static void Info(string message) => Log(message, "INFO");
        public static void Warn(string message) => Log(message, "WARN");
        public static void Error(string message, Exception? ex = null) => Log(ex != null ? $"{message}: {ex.Message}{Environment.NewLine}{ex.StackTrace}" : message, "ERROR");
    }
}
