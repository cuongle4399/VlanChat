using System;
using System.Text.Json.Serialization;

namespace LANChatPro.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string SenderId { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsPrivate { get; set; }
        public string RecipientId { get; set; } = string.Empty; // If private
        public string FilePath { get; set; } = string.Empty; // If file message
        public long FileSize { get; set; }
        
        [JsonIgnore]
        public string FormattedTimestamp => Timestamp.ToLocalTime().ToString("HH:mm:ss");
    }
}
