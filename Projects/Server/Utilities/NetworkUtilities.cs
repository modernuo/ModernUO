using System.Net;
using System.Net.Sockets;
using Server.Network;

namespace Server;

public static class NetworkUtilities
{
    public static bool IsPrivateNetwork(this IPAddress ip) =>
        ip.AddressFamily switch
        {
            AddressFamily.InterNetwork   => ip.IsPrivateNetworkV4(),
            AddressFamily.InterNetworkV6 => ip.IsPrivateNetworkV6(),
            _                            => false
        };

    private static readonly IFirewallEntry[] _privateNetworkV4 =
    [
        new CidrFirewallEntry("127.0.0.1/8"),
        new CidrFirewallEntry("192.168.0.0/16"),
        new CidrFirewallEntry("10.0.0.0/8"),
        new CidrFirewallEntry("172.16.0.0/12"),
        new CidrFirewallEntry("169.254.0.0/16"),
        new CidrFirewallEntry("100.64.0.0/10")
    ];

    private static readonly IFirewallEntry[] _privateNetworkV6 =
    [
        new CidrFirewallEntry("fc00::/7"),
        new CidrFirewallEntry("fe80::/10")
    ];

    public static bool IsPrivateNetworkV4(this IPAddress ip)
    {
        for (var i = 0; i < _privateNetworkV4.Length; i++)
        {
            if (_privateNetworkV4[i].IsBlocked(ip))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsPrivateNetworkV6(this IPAddress ip) =>
        _privateNetworkV6[0].IsBlocked(ip) ||
        _privateNetworkV6[1].IsBlocked(ip);
}
