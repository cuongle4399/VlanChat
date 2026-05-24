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
    public class ServerConnection
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private CancellationTokenSource? _cts;
        private readonly string _serverIp;
        private readonly int _serverPort;
        
        public event Action<NetworkMessage>? MessageReceived;
        public event Action? Disconnected;

        public ServerConnection(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public async Task<bool> ConnectAsync(NetworkMessage joinMessage)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_serverIp, _serverPort);
                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                _cts = new CancellationTokenSource();
                
                // Send join message
                await SendMessageAsync(joinMessage);
                
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to connect to server {_serverIp}:{_serverPort}", ex);
                Disconnect();
                return false;
            }
        }

        public async Task<bool> SendMessageAsync(NetworkMessage msg)
        {
            if (_writer == null) return false;
            try
            {
                string json = JsonSerializer.Serialize(msg, JsonContext.Default.NetworkMessage);
                await _writer.WriteLineAsync(json);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error sending message to server", ex);
                Disconnect();
                return false;
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _reader != null)
                {
                    string? json = await _reader.ReadLineAsync(token);
                    if (string.IsNullOrEmpty(json))
                        break; // Server closed connection

                    var msg = JsonSerializer.Deserialize(json, JsonContext.Default.NetworkMessage);
                    if (msg != null)
                    {
                        MessageReceived?.Invoke(msg);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Error("Error receiving data from server", ex);
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_client == null) return;
            
            _cts?.Cancel();
            _reader?.Dispose();
            _writer?.Dispose();
            _stream?.Dispose();
            _client?.Close();
            
            _client = null;
            Disconnected?.Invoke();
        }
    }
}
