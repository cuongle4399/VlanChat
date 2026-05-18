using System;
using System.Text.Json.Serialization;

namespace LANChatPro.Models
{
    public enum FileTransferDirection
    {
        Send,
        Receive
    }

    public enum FileTransferStatus
    {
        Pending,
        Accepted,
        Rejected,
        Transferring,
        Completed,
        Failed,
        Cancelled
    }

    public class FileTransferInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string PeerId { get; set; } = string.Empty;
        public string PeerUsername { get; set; } = string.Empty;
        public string PeerIp { get; set; } = string.Empty;
        public int PeerPort { get; set; }
        public FileTransferDirection Direction { get; set; }
        public FileTransferStatus Status { get; set; } = FileTransferStatus.Pending;
        public long BytesTransferred { get; set; }
        public double SpeedBytesPerSecond { get; set; }
        public string SpeedString { get; set; } = "0 B/s";
        public string ElapsedTimeString { get; set; } = "00:00";
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public double ProgressPercentage => FileSize > 0 ? ((double)BytesTransferred / FileSize) * 100 : 0;
    }
}
