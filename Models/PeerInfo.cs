using System;
using System.Text.Json.Serialization;

namespace LANChatPro.Models
{
    public class PeerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public int AvatarIndex { get; set; }
        public string Status { get; set; } = "online";
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime OnlineSince { get; set; } = DateTime.Now;
        public bool IsTyping { get; set; }
        public DateTime TypingStartTime { get; set; } = DateTime.MinValue;

        [JsonIgnore]
        public bool IsOnline => (DateTime.UtcNow - LastSeen).TotalSeconds < 15;
    }
}
