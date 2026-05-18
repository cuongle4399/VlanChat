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
        private DateTime _lastSubnetProbeUtc = DateTime.MinValue;

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

Task.Run(() => ListenAsync(_cts.Token));

Task.Run(() => HeartbeatLoopAsync(_cts.Token));

            Logger.Info("UDP Discovery Service started.");
        }

        public void Stop()
        {

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

                        MessageReceived?.Invoke(msg, senderIp);
                    }
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException || ex is OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Logger.Error("Error in UDP listener socket loop", ex);
            }
        }

        private async Task HeartbeatLoopAsync(CancellationToken token)
        {

            BroadcastHello();

            try
            {
                while (!token.IsCancellationRequested)
                {

                    await Task.Delay(5000, token);
                    BroadcastHeartbeat();

                    if ((DateTime.UtcNow - _lastSubnetProbeUtc).TotalSeconds >= 15)
                    {
                        _lastSubnetProbeUtc = DateTime.UtcNow;
                        ProbeSubnetByUnicast();
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
        }

        private void BroadcastMessage(NetworkMessage msg)
        {
            try
            {
                foreach (var endpoint in GetBroadcastEndpoints())
                {
                    SendMessageToEndpoint(msg, endpoint, enableBroadcast: true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in UDP broadcast payload delivery", ex);
            }
        }

        private void ProbeSubnetByUnicast()
        {
            try
            {
                var msg = _messageFactory();
                msg.Type = "HELLO";

                foreach (IPAddress ip in Helpers.GetLocalSubnetIPv4Addresses(maxHosts: 512))
                {
                    SendMessageToEndpoint(msg, new IPEndPoint(ip, UdpPort), enableBroadcast: false);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"UDP unicast subnet probe failed: {ex.Message}");
            }
        }

        private void SendMessageToEndpoint(NetworkMessage msg, IPEndPoint endpoint, bool enableBroadcast)
        {
            _udpSender ??= new UdpClient();
            _udpSender.EnableBroadcast = enableBroadcast;

            string json = JsonSerializer.Serialize(msg, JsonContext.Default.NetworkMessage);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            _udpSender.Send(bytes, bytes.Length, endpoint);
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
