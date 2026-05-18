using System;

namespace LANChatPro.Models
{
    public class NetworkMessage
    {
        public string Type { get; set; } = string.Empty; // HELLO, HEARTBEAT, GOODBYE, CHAT, TYPING, FILE_REQ, FILE_RESP
        public string SenderId { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string SenderMachineName { get; set; } = string.Empty;
        public int SenderPort { get; set; }
        public int SenderAvatarIndex { get; set; }
        public string Content { get; set; } = string.Empty; // Message body or JSON metadata
        public string FileId { get; set; } = string.Empty; // File unique identifier
        public string FileName { get; set; } = string.Empty; // File metadata
        public long FileSize { get; set; } // File metadata
        public int FilePort { get; set; } // Temporary TCP port for file stream payloads
        public bool IsPrivate { get; set; }
        public string RecipientId { get; set; } = string.Empty;
    }
}
