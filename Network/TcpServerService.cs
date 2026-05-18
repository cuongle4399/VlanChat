using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LANChatPro.Models;
using LANChatPro.Utils;

namespace LANChatPro.Network
{
    public class TcpServerService
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly int _port;

        public event Action<NetworkMessage, string>? MessageReceived;

        public TcpServerService(int port)
        {
            _port = port;
        }

        public void Start()
        {
            if (_cts != null)
                return;

            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            _cts = new CancellationTokenSource();
            Task.Run(() => ListenAsync(_cts.Token));
            Logger.Info($"TCP Server Service started on port {_port}.");
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _cts?.Dispose();
            _cts = null;
            _listener = null;
            Logger.Info("TCP Server Service stopped.");
        }

        private async Task ListenAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_listener == null)
                        break;

                    TcpClient client = await _listener.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleClientAsync(client, token));
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Logger.Error("Error in TCP server listener network loop", ex);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            string clientIp = "Unknown";
            try
            {
                if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
                {
                    clientIp = endPoint.Address.ToString();
                }

                using (client)
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string payload = await reader.ReadToEndAsync(token);
                    if (string.IsNullOrWhiteSpace(payload))
                        return;

                    var msg = JsonSerializer.Deserialize(payload, JsonContext.Default.NetworkMessage);
                    if (msg != null)
                    {
                        MessageReceived?.Invoke(msg, clientIp);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Logger.Error($"Error executing worker handling for client {clientIp}", ex);
            }
        }
    }
}
