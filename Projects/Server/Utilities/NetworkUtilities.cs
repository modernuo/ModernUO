using System;
using System.Net;
using System.Net.Sockets;
using Server.Collections;

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

    // These are constant reserved ranges, not firewall entries -- they only ever answer "is this address
    // in one of these blocks?", which is exactly what SortedRangeIndex is for. Building them through the
    // firewall entry types was a convenience that made core depend on the firewall for something that has
    // nothing to do with banning.
    private static readonly SortedRangeIndex<UInt128> _privateNetworkV4 = BuildIndex(
        "127.0.0.1/8",
        "192.168.0.0/16",
        "10.0.0.0/8",
        "172.16.0.0/12",
        "169.254.0.0/16",
        "100.64.0.0/10"
    );

    private static readonly SortedRangeIndex<UInt128> _privateNetworkV6 = BuildIndex(
        "fc00::/7",
        "fe80::/10"
    );

    private static SortedRangeIndex<UInt128> BuildIndex(params ReadOnlySpan<string> cidrs)
    {
        var ranges = new SortedRangeIndex<UInt128>.Range[cidrs.Length];
        for (var i = 0; i < cidrs.Length; i++)
        {
            if (!IPAddressUtility.TryParseCidrRange(cidrs[i], out var min, out var max))
            {
                throw new ArgumentException($"Invalid reserved-network CIDR \"{cidrs[i]}\"");
            }

            ranges[i] = new SortedRangeIndex<UInt128>.Range(min, max);
        }

        Array.Sort(ranges, SortedRangeIndex<UInt128>.ByMin);
        return SortedRangeIndex<UInt128>.Build(ranges);
    }

    public static bool IsPrivateNetworkV4(this IPAddress ip) => _privateNetworkV4.Contains(ip.ToUInt128());

    public static bool IsPrivateNetworkV6(this IPAddress ip) => _privateNetworkV6.Contains(ip.ToUInt128());
}
