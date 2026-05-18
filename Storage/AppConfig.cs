using System;

namespace LANChatPro.Storage
{
    public class AppConfig
    {
        public string Username { get; set; } = string.Empty;
        public int AvatarIndex { get; set; } = 0;
        public string DownloadFolder { get; set; } = string.Empty;
        public bool EnableSound { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public int Port { get; set; } = 50002;
    }
}
