/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CidrFirewallEntry.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Net;
using System.Net.Sockets;

namespace Server.Network;

public class CidrFirewallEntry : BaseFirewallEntry
{
    public override UInt128 MinIpAddress { get; }
    public override UInt128 MaxIpAddress { get; }

    public CidrFirewallEntry(string ipAddressOrCidr)
        : this(ParseIPAddress(ipAddressOrCidr, out var prefixLength), prefixLength)
    {
    }

    public CidrFirewallEntry(IPAddress minAddress, IPAddress maxAddress)
    {
        MinIpAddress = minAddress.ToUInt128();
        MaxIpAddress = maxAddress.ToUInt128();
    }

    public CidrFirewallEntry(IPAddress ipAddress, int prefixLength)
    {
        Span<byte> bytes = stackalloc byte[16];

        if (ipAddress.AddressFamily != AddressFamily.InterNetworkV6)
        {
            prefixLength += 96; // 32 -> 128
        }

        ipAddress.WriteMappedIPv6To(bytes);

        MinIpAddress = Utility.CreateCidrAddress(bytes, prefixLength, false);
        MaxIpAddress = Utility.CreateCidrAddress(bytes, prefixLength, true);
    }

    private static IPAddress ParseIPAddress(ReadOnlySpan<char> ipString, out int prefixLength)
    {
        int slashIndex = ipString.IndexOf('/');
        var ipAddress = IPAddress.Parse(slashIndex > -1 ? ipString[..slashIndex] : ipString);
        var maxPrefixLength = ipAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;

        if (slashIndex == -1)
        {
            prefixLength = maxPrefixLength;
        }
        else
        {
            var prefixPart = ipString[(slashIndex + 1)..];

            if (!int.TryParse(prefixPart, out prefixLength) || prefixLength < 0 || prefixLength > maxPrefixLength)
            {
                throw new ArgumentException("Invalid prefix length.");
            }
        }

        return ipAddress;
    }
}
