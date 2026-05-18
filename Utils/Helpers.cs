using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LANChatPro.Utils
{
    public static class Helpers
    {
        public static string GetLocalIPAddress()
        {
            try
            {

                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        string desc = ni.Description.ToLower();

                        if (desc.Contains("virtual") || desc.Contains("pseudo") || desc.Contains("vmware") ||
                            desc.Contains("hyper-v") || desc.Contains("virtualbox") || desc.Contains("host-only") ||
                            desc.Contains("wsl") || desc.Contains("vpn"))
                        {
                            continue;
                        }

                        IPInterfaceProperties ipProps = ni.GetIPProperties();
                        foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return addr.Address.ToString();
                            }
                        }
                    }
                }

using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                    if (endPoint != null)
                    {
                        return endPoint.Address.ToString();
                    }
                }
            }
            catch
            {

            }

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {

            }

            return "127.0.0.1";
        }

        public static IReadOnlyList<IPAddress> GetLocalSubnetIPv4Addresses(int maxHosts = 512)
        {
            var addresses = new List<IPAddress>();
            var localAddresses = new HashSet<string>(
                GetLocalIPv4Addresses().Select(ip => ip.ToString()),
                StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    string desc = ni.Description.ToLowerInvariant();
                    if (desc.Contains("virtual") || desc.Contains("pseudo") || desc.Contains("vmware") ||
                        desc.Contains("hyper-v") || desc.Contains("virtualbox") || desc.Contains("host-only") ||
                        desc.Contains("wsl") || desc.Contains("vpn"))
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily != AddressFamily.InterNetwork || addr.IPv4Mask == null)
                            continue;

                        foreach (IPAddress ip in EnumerateSubnet(addr.Address, addr.IPv4Mask, maxHosts))
                        {
                            string ipText = ip.ToString();
                            if (!localAddresses.Contains(ipText))
                            {
                                addresses.Add(ip);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return addresses
                .GroupBy(ip => ip.ToString())
                .Select(group => group.First())
                .ToArray();
        }

        public static IReadOnlyList<IPAddress> GetLocalIPv4Addresses()
        {
            var addresses = new List<IPAddress>();
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
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            addresses.Add(addr.Address);
                        }
                    }
                }
            }
            catch
            {
            }

            return addresses;
        }

        private static IEnumerable<IPAddress> EnumerateSubnet(IPAddress address, IPAddress mask, int maxHosts)
        {
            uint ip = ToUInt32(address);
            uint subnetMask = ToUInt32(mask);
            uint network = ip & subnetMask;
            uint broadcast = network | ~subnetMask;

            if (broadcast <= network + 1)
                yield break;

            uint availableHosts = broadcast - network - 1;
            uint limit = (uint)Math.Min(maxHosts, availableHosts);

            uint start = network + 1;
            uint end = network + limit;

            if (ip > end && availableHosts > limit)
            {
                uint half = limit / 2;
                start = ip > half ? ip - half : network + 1;
                end = Math.Min(broadcast - 1, start + limit - 1);
            }

            for (uint current = start; current <= end; current++)
            {
                yield return FromUInt32(current);
            }
        }

        private static uint ToUInt32(IPAddress ip)
        {
            byte[] bytes = ip.GetAddressBytes();
            return ((uint)bytes[0] << 24) |
                   ((uint)bytes[1] << 16) |
                   ((uint)bytes[2] << 8) |
                   bytes[3];
        }

        private static IPAddress FromUInt32(uint value)
        {
            return new IPAddress(new[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            });
        }

        public static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = Math.Max(0, bytes);
            while (number >= 1024 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}
