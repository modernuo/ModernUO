/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IPAddressUtility.cs                                             *
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
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace Server;

/// <summary>
/// Low-level IPAddress conversion and parsing helpers shared by the firewall, ban channel, and
/// blocklist. All members are allocation-free (stack buffers only) so they are safe on hot accept
/// paths and inside tight parse loops.
/// </summary>
public static class IPAddressUtility
{
    // Converts an IPAddress to a UInt128 in IPv6 format
    public static UInt128 ToUInt128(this IPAddress ip)
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.IsIPv4MappedToIPv6)
        {
            Span<byte> integer = stackalloc byte[4];
            return !ip.TryWriteBytes(integer, out _)
                ? (UInt128)0
                : new UInt128(0, 0xFFFF00000000UL | BinaryPrimitives.ReadUInt32BigEndian(integer));
        }

        Span<byte> bytes = stackalloc byte[16];
        if (!ip.TryWriteBytes(bytes, out _))
        {
            return 0;
        }

        var high = BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]);
        var low = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(8, 8));

        return new UInt128(high, low);
    }

    // Converts a UInt128 in IPv6 format to an IPAddress
    public static IPAddress ToIpAddress(this UInt128 value, bool mapToIpv6 = false)
    {
        // IPv4 mapped IPv6 address
        if (!mapToIpv6 && value >= 0xFFFF00000000UL && value <= 0xFFFFFFFFFFFFUL)
        {
            var newAddress = IPAddress.HostToNetworkOrder((int)value);
            return new IPAddress(unchecked((uint)newAddress));
        }

        Span<byte> bytes = stackalloc byte[16]; // 128 bits for IPv6 address
        ((IBinaryInteger<UInt128>)value).WriteBigEndian(bytes);

        return new IPAddress(bytes);
    }

    /// <summary>Extracts the big-endian uint of an <see cref="AddressFamily.InterNetwork"/> address.</summary>
    public static bool TryV4(IPAddress ip, out uint v)
    {
        Span<byte> b = stackalloc byte[4];
        if (ip.TryWriteBytes(b, out var n) && n == 4)
        {
            v = ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
            return true;
        }

        v = 0;
        return false;
    }

    /// <summary>
    /// Extracts the embedded v4 uint from a v4-mapped-v6 address directly from the mapped bytes,
    /// avoiding the allocation of <see cref="IPAddress.MapToIPv4"/>.
    /// </summary>
    public static bool TryMappedV4(IPAddress ip, out uint v)
    {
        Span<byte> b = stackalloc byte[16];
        if (ip.TryWriteBytes(b, out var n) && n == 16)
        {
            v = ((uint)b[12] << 24) | ((uint)b[13] << 16) | ((uint)b[14] << 8) | b[15];
            return true;
        }

        v = 0;
        return false;
    }

    /// <summary>Parses a dotted-quad IPv4 literal into a big-endian uint. Allocation-free, strict.</summary>
    public static bool TryParseV4(ReadOnlySpan<char> s, out uint v)
    {
        v = 0;
        uint acc = 0;
        int octet = 0, digits = 0, dots = 0;
        foreach (var c in s)
        {
            if (c == '.')
            {
                if (digits == 0 || octet > 255)
                {
                    return false;
                }

                acc = (acc << 8) | (uint)octet;
                dots++;
                octet = 0;
                digits = 0;
            }
            else if (c is >= '0' and <= '9')
            {
                octet = octet * 10 + (c - '0');
                if (++digits > 3)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        if (dots != 3 || digits == 0 || octet > 255)
        {
            return false;
        }

        v = (acc << 8) | (uint)octet;
        return true;
    }

    /// <summary>
    /// UTF-8/ASCII byte overload of <see cref="TryParseV4(ReadOnlySpan{char}, out uint)"/>, mirroring its
    /// validation exactly so the blocklist can parse dotted-quads straight from file bytes with no
    /// per-line string allocation.
    /// </summary>
    public static bool TryParseV4(ReadOnlySpan<byte> s, out uint v)
    {
        v = 0;
        uint acc = 0;
        int octet = 0, digits = 0, dots = 0;
        foreach (var c in s)
        {
            if (c == (byte)'.')
            {
                if (digits == 0 || octet > 255)
                {
                    return false;
                }

                acc = (acc << 8) | (uint)octet;
                dots++;
                octet = 0;
                digits = 0;
            }
            else if (c is >= (byte)'0' and <= (byte)'9')
            {
                octet = octet * 10 + (c - '0');
                if (++digits > 3)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        if (dots != 3 || digits == 0 || octet > 255)
        {
            return false;
        }

        v = (acc << 8) | (uint)octet;
        return true;
    }
}
