using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ModernUO.CodeGeneratedEvents;
using Server.Logging;
using Server.Network;

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

        private static bool _useServerListingAddressConfig { get; set; }

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

                if (_publicAddress != null)
                {
                    _useServerListingAddressConfig = true;

                    logger.Information("Server listing address set from config: {address}", _publicAddress);
                }
            }
        }

        [OnEvent(nameof(GatewayServer.ServerListEvent))]
        public static void OnServerListEvent(GatewayServer.ServerListEventArgs e)
        {
            try
            {
                var ns = e.State;

                var localEndPoint = ns.LocalEndPoint;
                if (localEndPoint == null)
                {
                    return;
                }

                var localAddress = localEndPoint.Address;
                var localPort = localEndPoint.Port;

                if (_useServerListingAddressConfig)
                {
                    localAddress = _publicAddress;
                }
                else if (localAddress.IsPrivateNetwork())
                {
                    // Check if client is from a public network
                    if (!ns.Address.IsPrivateNetwork() && _publicAddress != null)
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
                    logger.Error("Could not auto-detect public IP address. Users will not be able to connect!");
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
                        !ip.IsPrivateNetwork())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private const string _ipifyUrl = "https://api.ipify.org";

        private static IPAddress FindPublicAddress()
        {
            const int count = 3;
            for (var i = 0; i < count; i++)
            {
                try
                {
                    // This isn't called often so we don't need to optimize
                    using var hc = new HttpClient();
                    hc.Timeout = TimeSpan.FromSeconds(1); // Only wait 1 second
                    var ipAddress = hc.GetStringAsync(_ipifyUrl).Result;
                    return IPAddress.Parse(ipAddress);
                }
                catch
                {
                    // ignored
                }
            }

            logger.Warning("Attempted to get a public IP address {Count} times from {RemoteIPService} and failed.", count, _ipifyUrl);
            return null;
        }
    }
}
