using System;

namespace LANChatPro.Models
{
    public class PendingFileDownload
    {
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string SenderIp { get; set; } = string.Empty;
        public int FilePort { get; set; }
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }
}
