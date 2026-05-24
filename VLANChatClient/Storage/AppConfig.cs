using System;

namespace LANChatPro.Storage
{
    public class AppConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int AvatarIndex { get; set; } = 0;
        public string DownloadFolder { get; set; } = string.Empty;
        public bool EnableSound { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public int Port { get; set; } = 50002;
        public string ServerIp { get; set; } = "127.0.0.1";
        public int ServerPort { get; set; } = 5000;
    }
}
