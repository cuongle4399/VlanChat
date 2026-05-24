using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LANChatPro.Models;
using LANChatPro.Utils;

namespace LANChatPro.Services
{
    public class FileTransferService
    {
        public ConcurrentDictionary<string, FileTransferInfo> Transfers { get; } = new();

        public event Action<FileTransferInfo>? TransferUpdated;
        public event Action<FileTransferInfo>? TransferCompleted;
        public event Action<FileTransferInfo, string>? TransferFailed;

        public FileTransferService()
        {
        }

        public async Task StartSendSessionAsync(string filePath, string peerId, string peerUsername, string serverIp, int serverPort, Func<string, Task<bool>> sendRequestAsync)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("File not found on system", filePath);

            var transfer = new FileTransferInfo
            {
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                FilePath = filePath,
                PeerId = peerId,
                PeerUsername = peerUsername,
                PeerIp = serverIp,
                Direction = FileTransferDirection.Send,
                Status = FileTransferStatus.Pending,
                StartTime = DateTime.UtcNow
            };

            Transfers[transfer.Id] = transfer;
            TransferUpdated?.Invoke(transfer);

            try
            {
                transfer.Status = FileTransferStatus.Transferring;
                TransferUpdated?.Invoke(transfer);

                // Connect to server file port (serverPort + 1)
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(serverIp, serverPort + 1);
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Send upload header
                        byte[] headerBytes = Encoding.UTF8.GetBytes($"UPLOAD|{transfer.Id}|{transfer.FileName}|{transfer.FileSize}\n");
                        await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

                        // Stream bytes
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            byte[] buffer = new byte[65536];
                            int read;
                            long totalSent = 0;
                            DateTime lastSpeedCheck = DateTime.UtcNow;
                            long bytesSentSinceLastCheck = 0;

                            while ((read = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await stream.WriteAsync(buffer, 0, read);
                                totalSent += read;
                                bytesSentSinceLastCheck += read;

                                transfer.BytesTransferred = totalSent;

                                var now = DateTime.UtcNow;
                                var elapsed = (now - lastSpeedCheck).TotalSeconds;
                                if (elapsed >= 0.5)
                                {
                                    double speed = bytesSentSinceLastCheck / elapsed;
                                    transfer.SpeedBytesPerSecond = speed;
                                    transfer.SpeedString = Helpers.FormatFileSize((long)speed) + "/s";

                                    var duration = now - transfer.StartTime;
                                    transfer.ElapsedTimeString = $"{duration.Minutes:D2}:{duration.Seconds:D2}";

                                    lastSpeedCheck = now;
                                    bytesSentSinceLastCheck = 0;
                                    TransferUpdated?.Invoke(transfer);
                                }
                            }
                        }

                        // Read response
                        string? response = await ReadLineAsync(stream);
                        if (response != "OK")
                        {
                            throw new IOException($"Server rejected upload: {response}");
                        }
                    }
                }

                // File uploaded successfully, now send the chat metadata message!
                bool metadataSent = await sendRequestAsync(transfer.Id);
                if (!metadataSent)
                {
                    throw new IOException("Failed to send file metadata to server.");
                }

                transfer.Status = FileTransferStatus.Completed;
                transfer.SpeedString = "Done";
                TransferUpdated?.Invoke(transfer);
                TransferCompleted?.Invoke(transfer);
                Transfers.TryRemove(transfer.Id, out _);
            }
            catch (Exception ex)
            {
                transfer.Status = FileTransferStatus.Failed;
                Transfers.TryRemove(transfer.Id, out _);
                TransferFailed?.Invoke(transfer, ex.Message);
                throw;
            }
        }

        public async Task StartDownloadSessionAsync(string fileId, string savePath, string fileName, long fileSize, string serverIp, int serverPort)
        {
            var transfer = new FileTransferInfo
            {
                Id = fileId,
                FileName = fileName,
                FileSize = fileSize,
                FilePath = savePath,
                PeerId = "server",
                PeerUsername = "Máy chủ",
                PeerIp = serverIp,
                Direction = FileTransferDirection.Receive,
                Status = FileTransferStatus.Pending,
                StartTime = DateTime.UtcNow
            };

            Transfers[transfer.Id] = transfer;
            TransferUpdated?.Invoke(transfer);

            try
            {
                transfer.Status = FileTransferStatus.Transferring;
                TransferUpdated?.Invoke(transfer);

                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(serverIp, serverPort + 1);
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Send download header
                        byte[] headerBytes = Encoding.UTF8.GetBytes($"DOWNLOAD|{fileId}\n");
                        await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

                        // Read response header
                        string? response = await ReadLineAsync(stream);
                        if (string.IsNullOrEmpty(response) || !response.StartsWith("OK|"))
                        {
                            string errMsg = response?.StartsWith("ERROR|") == true ? response.Substring(6) : "Unknown error";
                            throw new IOException(errMsg);
                        }

                        long responseSize = long.Parse(response.Split('|')[1]);

                        // Stream file bytes to disk
                        using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            byte[] buffer = new byte[65536];
                            long totalRead = 0;
                            int read;
                            DateTime lastSpeedCheck = DateTime.UtcNow;
                            long bytesReadSinceLastCheck = 0;

                            while (totalRead < responseSize)
                            {
                                int toRead = (int)Math.Min(buffer.Length, responseSize - totalRead);
                                read = await stream.ReadAsync(buffer, 0, toRead);
                                if (read == 0)
                                    throw new IOException("Server disconnected prematurely during download.");

                                await fileStream.WriteAsync(buffer, 0, read);
                                totalRead += read;
                                bytesReadSinceLastCheck += read;

                                transfer.BytesTransferred = totalRead;

                                var now = DateTime.UtcNow;
                                var elapsed = (now - lastSpeedCheck).TotalSeconds;
                                if (elapsed >= 0.5)
                                {
                                    double speed = bytesReadSinceLastCheck / elapsed;
                                    transfer.SpeedBytesPerSecond = speed;
                                    transfer.SpeedString = Helpers.FormatFileSize((long)speed) + "/s";

                                    var duration = now - transfer.StartTime;
                                    transfer.ElapsedTimeString = $"{duration.Minutes:D2}:{duration.Seconds:D2}";

                                    lastSpeedCheck = now;
                                    bytesReadSinceLastCheck = 0;
                                    TransferUpdated?.Invoke(transfer);
                                }
                            }
                        }
                    }
                }

                transfer.Status = FileTransferStatus.Completed;
                transfer.SpeedString = "Done";
                TransferUpdated?.Invoke(transfer);
                TransferCompleted?.Invoke(transfer);
                Transfers.TryRemove(transfer.Id, out _);
            }
            catch (Exception ex)
            {
                transfer.Status = FileTransferStatus.Failed;
                Transfers.TryRemove(transfer.Id, out _);
                TransferFailed?.Invoke(transfer, ex.Message);
                throw;
            }
        }

        public void RejectSendSession(string transferId)
        {
            // Keep for interface compatibility
        }

        public void StartReceiveSession(string transferId, string fileName, long fileSize, string peerId, string peerUsername, string senderIp, int senderPort, string savePath)
        {
            // Keep for interface compatibility
        }

        private static async Task<string?> ReadLineAsync(NetworkStream stream)
        {
            var bytes = new System.Collections.Generic.List<byte>();
            byte[] buf = new byte[1];
            while (true)
            {
                int read = await stream.ReadAsync(buf, 0, 1);
                if (read == 0)
                {
                    if (bytes.Count == 0) return null;
                    break;
                }
                byte b = buf[0];
                if (b == '\n') break;
                if (b != '\r') bytes.Add(b);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }
}
