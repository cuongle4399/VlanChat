using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LANChatPro.Models;
using LANChatPro.Utils;

namespace LANChatPro.Network
{
    public class UdpDiscoveryService
    {
        private const int UdpPort = 50001;
        private UdpClient? _udpListener;
        private UdpClient? _udpSender;
        private CancellationTokenSource? _cts;
        private readonly string _localIp;
        private readonly int _tcpPort;
        private readonly Func<NetworkMessage> _messageFactory;

        public event Action<NetworkMessage, string>? MessageReceived;

        public UdpDiscoveryService(string localIp, int tcpPort, Func<NetworkMessage> messageFactory)
        {
            _localIp = localIp;
            _tcpPort = tcpPort;
            _messageFactory = messageFactory;
        }

        public void Start()
        {
            if (_cts != null)
                return;

            _cts = new CancellationTokenSource();
            
            // Start background listening task
            Task.Run(() => ListenAsync(_cts.Token));
            
            // Start background broadcast task
            Task.Run(() => HeartbeatLoopAsync(_cts.Token));

            Logger.Info("UDP Discovery Service started.");
        }

        public void Stop()
        {
            // Send Goodbye so other peers immediately know we went offline
            BroadcastGoodbye();

            _cts?.Cancel();
            _udpListener?.Dispose();
            _udpSender?.Dispose();
            _cts?.Dispose();
            _cts = null;
            _udpListener = null;
            _udpSender = null;
            Logger.Info("UDP Discovery Service stopped.");
        }

        private async Task ListenAsync(CancellationToken token)
        {
            try
            {
                _udpListener = new UdpClient();
                // Set Socket ReuseAddress option so multiple instances can run on the same PC
                _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, UdpPort));

                while (!token.IsCancellationRequested)
                {
                    var result = await _udpListener.ReceiveAsync(token);
                    string senderIp = result.RemoteEndPoint.Address.ToString();

                    string json = Encoding.UTF8.GetString(result.Buffer);
                    var msg = JsonSerializer.Deserialize(json, JsonContext.Default.NetworkMessage);
                    if (msg != null)
                    {
                        // Raise event to be handled in ChatService
                        MessageReceived?.Invoke(msg, senderIp);
                    }
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is OperationCanceledException)
            {
                // Graceful cancellation shutdown
            }
            catch (Exception ex)
            {
                Logger.Error("Error in UDP listener socket loop", ex);
            }
        }

        private async Task HeartbeatLoopAsync(CancellationToken token)
        {
            // Broadcast initial HELLO right away
            BroadcastHello();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Delay 5s between broadcasts
                    await Task.Delay(5000, token);
                    BroadcastHeartbeat();
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful cancellation shutdown
            }
        }

        private void BroadcastMessage(NetworkMessage msg)
        {
            try
            {
                _udpSender ??= new UdpClient();
                _udpSender.EnableBroadcast = true;

                string json = JsonSerializer.Serialize(msg, JsonContext.Default.NetworkMessage);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                foreach (var endpoint in GetBroadcastEndpoints())
                {
                    _udpSender.Send(bytes, bytes.Length, endpoint);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in UDP broadcast payload delivery", ex);
            }
        }

        private static IPEndPoint[] GetBroadcastEndpoints()
        {
            var endpoints = new List<IPEndPoint>
            {
                new IPEndPoint(IPAddress.Broadcast, UdpPort)
            };

            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork ||
                            addr.IPv4Mask == null)
                        {
                            continue;
                        }

                        byte[] ipBytes = addr.Address.GetAddressBytes();
                        byte[] maskBytes = addr.IPv4Mask.GetAddressBytes();
                        byte[] broadcastBytes = new byte[4];

                        for (int i = 0; i < broadcastBytes.Length; i++)
                        {
                            broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
                        }

                        endpoints.Add(new IPEndPoint(new IPAddress(broadcastBytes), UdpPort));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Unable to enumerate directed broadcast endpoints: {ex.Message}");
            }

            return endpoints
                .GroupBy(e => e.Address)
                .Select(g => g.First())
                .ToArray();
        }

        public void BroadcastHello()
        {
            var msg = _messageFactory();
            msg.Type = "HELLO";
            BroadcastMessage(msg);
        }

        private void BroadcastHeartbeat()
        {
            var msg = _messageFactory();
            msg.Type = "HEARTBEAT";
            BroadcastMessage(msg);
        }

        private void BroadcastGoodbye()
        {
            var msg = _messageFactory();
            msg.Type = "GOODBYE";
            BroadcastMessage(msg);
        }
    }
}
