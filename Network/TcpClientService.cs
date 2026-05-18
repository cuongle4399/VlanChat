using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LANChatPro.Models;
using LANChatPro.Utils;

namespace LANChatPro.Network
{
    public static class TcpClientService
    {
        public static async Task<bool> SendMessageAsync(string ip, int port, NetworkMessage msg)
        {
            try
            {
                using TcpClient client = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await client.ConnectAsync(ip, port, cts.Token);

                using NetworkStream stream = client.GetStream();
                using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                string json = JsonSerializer.Serialize(msg, JsonContext.Default.NetworkMessage);
                await writer.WriteLineAsync(json);
                return true;
            }
            catch (OperationCanceledException)
            {
                Logger.Warn($"TCP connection timed out when sending payload to {ip}:{port}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to deliver TCP message payload to {ip}:{port}", ex);
                return false;
            }
        }
    }
}
