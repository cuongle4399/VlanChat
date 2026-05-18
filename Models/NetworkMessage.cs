using System;

namespace LANChatPro.Models
{
    public class NetworkMessage
    {
        public string Type { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string SenderMachineName { get; set; } = string.Empty;
        public int SenderPort { get; set; }
        public int SenderAvatarIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int FilePort { get; set; }
        public bool IsPrivate { get; set; }
        public string RecipientId { get; set; } = string.Empty;
    }
}
