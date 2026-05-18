using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LANChatPro.Models;
using LANChatPro.Utils;

namespace LANChatPro.Services
{
    public class FileTransferService
    {
        public ConcurrentDictionary<string, FileTransferInfo> Transfers { get; } = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _sendSessionCancellations = new();

        public event Action<FileTransferInfo>? TransferUpdated;
        public event Action<FileTransferInfo>? TransferCompleted;
        public event Action<FileTransferInfo, string>? TransferFailed;

        public FileTransferService()
        {
        }

        // Starts a temporary server waiting for client to connect and pull
        public async Task<int> StartSendSessionAsync(string filePath, string peerId, string peerUsername, string peerIp, Func<string, int, Task<bool>> sendRequestAsync)
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
                PeerIp = peerIp,
                Direction = FileTransferDirection.Send,
                Status = FileTransferStatus.Pending,
                StartTime = DateTime.UtcNow
            };

            Transfers[transfer.Id] = transfer;
            TransferUpdated?.Invoke(transfer);

            // Bind to a dynamically allocated free port (0 tells OS to assign a free port)
            TcpListener fileListener = new TcpListener(IPAddress.Any, 0);
            fileListener.Start();
            int dynamicPort = ((IPEndPoint)fileListener.LocalEndpoint).Port;
            var acceptCts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            _sendSessionCancellations[transfer.Id] = acceptCts;

            try
            {
                bool requestSent = await sendRequestAsync(transfer.Id, dynamicPort);
                if (!requestSent)
                {
                    throw new IOException("File request could not be delivered to the peer.");
                }
            }
            catch
            {
                transfer.Status = FileTransferStatus.Failed;
                Transfers.TryRemove(transfer.Id, out _);
                _sendSessionCancellations.TryRemove(transfer.Id, out _);
                acceptCts.Dispose();
                fileListener.Stop();
                TransferFailed?.Invoke(transfer, "File request could not be delivered to the peer.");
                throw;
            }

            // Wait in background for recipient to establish download stream
            _ = Task.Run(async () =>
            {
                TcpClient? client = null;
                try
                {
                    client = await fileListener.AcceptTcpClientAsync(acceptCts.Token);
                    fileListener.Stop(); // Port no longer needs to listen

                    if (transfer.Status == FileTransferStatus.Rejected || transfer.Status == FileTransferStatus.Cancelled)
                    {
                        client.Dispose();
                        return;
                    }

                    transfer.Status = FileTransferStatus.Transferring;
                    TransferUpdated?.Invoke(transfer);

                    await SendFileStreamAsync(client, transfer);
                }
                catch (OperationCanceledException)
                {
                    if (transfer.Status != FileTransferStatus.Rejected && transfer.Status != FileTransferStatus.Cancelled)
                    {
                        transfer.Status = FileTransferStatus.Failed;
                        Transfers.TryRemove(transfer.Id, out _);
                        TransferFailed?.Invoke(transfer, "Timed out waiting for the receiver to accept the file.");
                    }
                }
                catch (Exception ex)
                {
                    if (transfer.Status != FileTransferStatus.Rejected && transfer.Status != FileTransferStatus.Cancelled)
                    {
                        client?.Dispose();
                        transfer.Status = FileTransferStatus.Failed;
                        Transfers.TryRemove(transfer.Id, out _);
                        TransferFailed?.Invoke(transfer, ex.Message);
                    }
                }
                finally
                {
                    fileListener.Stop();
                    _sendSessionCancellations.TryRemove(transfer.Id, out _);
                    acceptCts.Dispose();
                }
            });

            return dynamicPort;
        }

        public void RejectSendSession(string transferId)
        {
            if (Transfers.TryGetValue(transferId, out var transfer))
            {
                transfer.Status = FileTransferStatus.Rejected;
                Transfers.TryRemove(transferId, out _);
                if (_sendSessionCancellations.TryRemove(transferId, out var cts))
                {
                    cts.Cancel();
                }
                TransferUpdated?.Invoke(transfer);
            }
        }

        // Recipient connects to sender's dynamic port to pull bytes
        public void StartReceiveSession(string transferId, string fileName, long fileSize, string peerId, string peerUsername, string senderIp, int senderPort, string savePath)
        {
            var transfer = new FileTransferInfo
            {
                Id = transferId,
                FileName = fileName,
                FileSize = fileSize,
                FilePath = savePath,
                PeerId = peerId,
                PeerUsername = peerUsername,
                PeerIp = senderIp,
                PeerPort = senderPort,
                Direction = FileTransferDirection.Receive,
                Status = FileTransferStatus.Transferring,
                StartTime = DateTime.UtcNow
            };

            Transfers[transfer.Id] = transfer;
            TransferUpdated?.Invoke(transfer);

            _ = Task.Run(async () =>
            {
                TcpClient? client = null;
                try
                {
                    client = new TcpClient();
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await client.ConnectAsync(senderIp, senderPort, cts.Token);

                    await ReceiveFileStreamAsync(client, transfer);
                }
                catch (OperationCanceledException)
                {
                    client?.Dispose();
                    transfer.Status = FileTransferStatus.Failed;
                    Transfers.TryRemove(transfer.Id, out _);
                    TransferFailed?.Invoke(transfer, "Timed out connecting to the sender's file stream.");
                }
                catch (Exception ex)
                {
                    client?.Dispose();
                    transfer.Status = FileTransferStatus.Failed;
                    Transfers.TryRemove(transfer.Id, out _);
                    TransferFailed?.Invoke(transfer, ex.Message);
                }
            });
        }

        private async Task SendFileStreamAsync(TcpClient client, FileTransferInfo transfer)
        {
            using (client)
            using (NetworkStream netStream = client.GetStream())
            using (FileStream fileStream = new FileStream(transfer.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] buffer = new byte[65536]; // 64KB standard high-performance chunks
                int bytesRead;
                long totalBytesSent = 0;
                
                DateTime lastSpeedCheck = DateTime.UtcNow;
                long bytesSentSinceLastCheck = 0;

                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await netStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesSent += bytesRead;
                    bytesSentSinceLastCheck += bytesRead;

                    transfer.BytesTransferred = totalBytesSent;

                    var now = DateTime.UtcNow;
                    var elapsed = (now - lastSpeedCheck).TotalSeconds;
                    if (elapsed >= 1.0)
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

                transfer.BytesTransferred = totalBytesSent;
                transfer.Status = FileTransferStatus.Completed;
                transfer.SpeedString = "Done";
                TransferUpdated?.Invoke(transfer);
                TransferCompleted?.Invoke(transfer);
                Transfers.TryRemove(transfer.Id, out _);
            }
        }

        private async Task ReceiveFileStreamAsync(TcpClient client, FileTransferInfo transfer)
        {
            string folder = Path.GetDirectoryName(transfer.FilePath) ?? string.Empty;
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using (client)
            using (NetworkStream netStream = client.GetStream())
            using (FileStream fileStream = new FileStream(transfer.FilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[65536];
                int bytesRead;
                long totalBytesReceived = 0;

                DateTime lastSpeedCheck = DateTime.UtcNow;
                long bytesReceivedSinceLastCheck = 0;

                while ((bytesRead = await netStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesReceived += bytesRead;
                    bytesReceivedSinceLastCheck += bytesRead;

                    transfer.BytesTransferred = totalBytesReceived;

                    var now = DateTime.UtcNow;
                    var elapsed = (now - lastSpeedCheck).TotalSeconds;
                    if (elapsed >= 1.0)
                    {
                        double speed = bytesReceivedSinceLastCheck / elapsed;
                        transfer.SpeedBytesPerSecond = speed;
                        transfer.SpeedString = Helpers.FormatFileSize((long)speed) + "/s";

                        var duration = now - transfer.StartTime;
                        transfer.ElapsedTimeString = $"{duration.Minutes:D2}:{duration.Seconds:D2}";

                        lastSpeedCheck = now;
                        bytesReceivedSinceLastCheck = 0;
                        TransferUpdated?.Invoke(transfer);
                    }
                }

                if (totalBytesReceived == transfer.FileSize)
                {
                    transfer.BytesTransferred = totalBytesReceived;
                    transfer.Status = FileTransferStatus.Completed;
                    transfer.SpeedString = "Done";
                    TransferUpdated?.Invoke(transfer);
                    TransferCompleted?.Invoke(transfer);
                    Transfers.TryRemove(transfer.Id, out _);
                }
                else
                {
                    transfer.Status = FileTransferStatus.Failed;
                    TransferFailed?.Invoke(transfer, "Stream terminated prematurely. File may be incomplete.");
                    Transfers.TryRemove(transfer.Id, out _);
                }
            }
        }
    }
}
