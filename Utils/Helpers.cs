using System;
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
                // Strategy 1: Look through active physical interfaces
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        string desc = ni.Description.ToLower();
                        // Ignore virtual adapters commonly created by Hyper-V, VirtualBox, VMware, etc.
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

                // Strategy 2: Attempt socket outbound connect simulation to query local end point
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
                // Fallback to basic DNS standard resolution
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
                // Absolute fallback
            }

            return "127.0.0.1";
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
