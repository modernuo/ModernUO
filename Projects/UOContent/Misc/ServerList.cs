using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Server.Logging;

namespace Server.Misc
{
    /*
     * The default settings are configured to automatically detect your external IP address.
     *
     * If your public IP address cannot be determined then set your IP address in modernuo.json, for example:
     * "serverListing.address": "1.2.3.4"
     *
     * If you do not plan on allowing clients outside of your LAN to connect, then set the following in modernuo.json
     * "serverListing.address": null,
     * "serverListing.autoDetect": false
     *
     * If you want players outside your LAN to be able to connect to your server and you are behind a router, you must also
     * forward TCP port 2593 to your private IP address. The procedure for doing this varies by manufacturer but generally
     * involves configuration of the router through your web browser.
     *
     * ServerList will direct connecting clients depending on both the address they are connecting from and the address and
     * port they are connecting to. If it is determined that both ends of a connection are private IP addresses, ServerList
     * will direct the client to the local private IP address. If a client is connecting to a local public IP address, they
     * will be directed to whichever address and port they initially connected to. This allows multi-homed servers to function
     * properly and fully supports listening on multiple ports. If a client with a public IP address is connecting to a
     * locally private address, the server will direct the client to either the automatically detected IP address or the
     * manually entered IP address or hostname, whichever is applicable. Loopback clients will be directed to loopback.
    */
    public static class ServerList
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(ServerList));

        private static IPAddress _publicAddress;
        public static string Address { get; private set; }
        public static string ServerName { get; private set; }

        public static bool AutoDetect { get; private set; }

        public static void Configure()
        {
            Address = ServerConfiguration.GetOrUpdateSetting("serverListing.address", null);
            AutoDetect = ServerConfiguration.GetOrUpdateSetting("serverListing.autoDetect", true);
            ServerName = ServerConfiguration.GetOrUpdateSetting("serverListing.serverName", "ModernUO");
        }

        public static void Initialize()
        {
            if (Address == null)
            {
                if (AutoDetect)
                {
                    AutoDetection();
                }
            }
            else
            {
                Resolve(Address, out _publicAddress);
            }

            EventSink.ServerList += EventSink_ServerList;
        }

        private static void EventSink_ServerList(ServerListEventArgs e)
        {
            try
            {
                var ns = e.State;

                var ipep = (IPEndPoint)ns.Connection?.LocalEndPoint;
                if (ipep == null)
                {
                    return;
                }

                var localAddress = ipep.Address;
                var localPort = ipep.Port;

                if (IsPrivateNetwork(localAddress))
                {
                    ipep = (IPEndPoint)ns.Connection.RemoteEndPoint;
                    if (ipep == null || !IsPrivateNetwork(ipep.Address) && _publicAddress != null)
                    {
                        localAddress = _publicAddress;
                    }
                }

                e.AddServer(ServerName, new IPEndPoint(localAddress, localPort));
            }
            catch (Exception er)
            {
                logger.Warning(er, "Unhandled exception at server list");
                e.Rejected = true;
            }
        }

        private static void AutoDetection()
        {
            if (!HasPublicIPAddress())
            {
                _publicAddress = FindPublicAddress();

                if (_publicAddress != null)
                {
                    logger.Information("Auto-detected public IP address ({IPAddress})", _publicAddress);
                }
                else
                {
                    logger.Warning("Could not auto-detect public IP address");
                }
            }
        }

        private static void Resolve(string addr, out IPAddress outValue)
        {
            if (IPAddress.TryParse(addr, out outValue))
            {
                return;
            }

            try
            {
                var iphe = Dns.GetHostEntry(addr);

                if (iphe.AddressList.Length > 0)
                {
                    outValue = iphe.AddressList[^1];
                }
            }
            catch
            {
                // ignored
            }
        }

        private static bool HasPublicIPAddress()
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    var ip = unicast.Address;
                    if (!IPAddress.IsLoopback(ip) && ip.AddressFamily != AddressFamily.InterNetworkV6 &&
                        !IsPrivateNetwork(ip))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 10.0.0.0/8
        // 172.16.0.0/12
        // 192.168.0.0/16
        // 169.254.0.0/16
        // 100.64.0.0/10 RFC 6598
        private static bool IsPrivateNetwork(IPAddress ip) =>
            ip.AddressFamily != AddressFamily.InterNetworkV6 &&
            (Utility.IPMatch("192.168.*", ip) ||
             Utility.IPMatch("10.*", ip) ||
             Utility.IPMatch("172.16-31.*", ip) ||
             Utility.IPMatch("169.254.*", ip) ||
             Utility.IPMatch("100.64-127.*", ip));

        private const string _ipifyUrl = "https://api.ipify.org";

        private static IPAddress FindPublicAddress()
        {
            try
            {
                // This isn't called often so we don't need to optimize
                using HttpClient hc = new HttpClient();
                var ipAddress = hc.GetStringAsync(_ipifyUrl).Result;
                return IPAddress.Parse(ipAddress);
            }
            catch
            {
                return null;
            }
        }
    }
}
