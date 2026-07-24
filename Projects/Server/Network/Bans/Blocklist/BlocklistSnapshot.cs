/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistSnapshot.cs                                            *
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
using System.Buffers.Text;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Server.Collections;

namespace Server.Network.Bans.Blocklist;

/// <summary>
/// Immutable dual-stack blocklist. Singles and CIDRs are folded into a single sorted, coalesced
/// interval index per family: IPv4 as <see cref="uint"/> ranges (lean for the millions-strong common
/// case), IPv6 as <see cref="UInt128"/> ranges (empty unless the feed carries v6). Immutable → lock-free reads.
/// </summary>
public sealed class BlocklistSnapshot
{
    public static readonly BlocklistSnapshot Empty = new(SortedRangeIndex<uint>.Empty, SortedRangeIndex<UInt128>.Empty);

    private readonly SortedRangeIndex<uint> _v4;
    private readonly SortedRangeIndex<UInt128> _v6;

    public int Count => _v4.Count + _v6.Count;

    private BlocklistSnapshot(SortedRangeIndex<uint> v4, SortedRangeIndex<UInt128> v6)
    {
        _v4 = v4;
        _v6 = v6;
    }

    /// <summary>
    /// Parses a blocklist directly from its UTF-8/ASCII file bytes — one line at a time, splitting on
    /// <c>'\n'</c> with no per-line string allocation. IPv4 singles and CIDRs are parsed straight from the
    /// byte span; IPv6 (the rare path) decodes the single address token and defers to the framework parser.
    /// Malformed lines increment <paramref name="skipped"/> and never throw. Build-time intermediates use
    /// the multithreaded pool because this runs off the game loop on the reload/bootstrap thread.
    /// </summary>
    public static BlocklistSnapshot Build(ReadOnlySpan<byte> data, out int parsed, out int skipped)
    {
        parsed = 0;
        skipped = 0;

        // Only the two final index arrays (allocated inside SortedRangeIndex.Build) hit the heap; every
        // build-time buffer here is a pooled ref list. mt: true is required — this runs off the game loop.
        using var v4 = PooledRefList<SortedRangeIndex<uint>.Range>.Create(mt: true);
        using var v6 = PooledRefList<SortedRangeIndex<UInt128>.Range>.Create(mt: true);

        var rest = data;
        while (!rest.IsEmpty)
        {
            ReadOnlySpan<byte> line;
            var nl = rest.IndexOf((byte)'\n');
            if (nl >= 0)
            {
                line = rest[..nl];
                rest = rest[(nl + 1)..];
            }
            else
            {
                line = rest;
                rest = default;
            }

            line = line[Ascii.Trim(line)];
            if (line.IsEmpty || line[0] == (byte)'#' || line[0] == (byte)';')
            {
                continue;
            }

            var slash = line.IndexOf((byte)'/');
            var addr = slash >= 0 ? line[..slash] : line;
            var bitsToken = slash >= 0 ? line[(slash + 1)..] : default;

            if (addr.IndexOf((byte)':') < 0)
            {
                // IPv4 single or CIDR — parsed straight from the byte span.
                if (slash >= 0)
                {
                    if (IPAddressUtility.TryParseV4(addr, out var ip) &&
                        TryParseBits(bitsToken, out var bits) && bits is >= 0 and <= 32)
                    {
                        var size = bits == 0 ? 0xFFFFFFFFu : (1u << (32 - bits)) - 1;
                        var b = ip & ~size;
                        v4.Add(new SortedRangeIndex<uint>.Range(b, b + size));
                        parsed++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else if (IPAddressUtility.TryParseV4(addr, out var ip))
                {
                    v4.Add(new SortedRangeIndex<uint>.Range(ip, ip));
                    parsed++;
                }
                else
                {
                    skipped++;
                }
            }
            else if (TryDecodeV6(addr, out var v))
            {
                // IPv6 is rare in these feeds; the single token was decoded and framework-parsed above.
                if (slash >= 0)
                {
                    if (TryParseBits(bitsToken, out var bits) && bits is >= 0 and <= 128)
                    {
                        var mask = bits == 0 ? UInt128.Zero : ~((UInt128.One << (128 - bits)) - 1);
                        var b = v & mask;
                        v6.Add(new SortedRangeIndex<UInt128>.Range(b, b | ~mask));
                        parsed++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else
                {
                    v6.Add(new SortedRangeIndex<UInt128>.Range(v, v));
                    parsed++;
                }
            }
            else
            {
                skipped++;
            }
        }

        v4.Sort(SortedRangeIndex<uint>.ByMin);
        v6.Sort(SortedRangeIndex<UInt128>.ByMin);
        return new BlocklistSnapshot(SortedRangeIndex<uint>.Build(v4.AsSpan()), SortedRangeIndex<UInt128>.Build(v6.AsSpan()));
    }

    // Decodes a single IPv6 address token from ASCII bytes and validates it via the framework parser.
    private static bool TryDecodeV6(ReadOnlySpan<byte> addr, out UInt128 v)
    {
        v = UInt128.Zero;
        if (addr.Length > 45)
        {
            return false;
        }

        Span<char> chars = stackalloc char[addr.Length];
        for (var i = 0; i < addr.Length; i++)
        {
            chars[i] = (char)addr[i];
        }

        if (!IPAddress.TryParse(chars, out var a) || a.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        v = a.ToUInt128();
        return true;
    }

    private static bool TryParseBits(ReadOnlySpan<byte> token, out int bits)
    {
        if (Utf8Parser.TryParse(token, out bits, out var consumed) && consumed == token.Length)
        {
            return true;
        }

        bits = 0;
        return false;
    }

    public bool IsBanned(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
        {
            // v6-encoded v4 must not dodge the v4 set; extract the embedded v4 uint directly.
            return IPAddressUtility.TryMappedV4(ip, out var mv) && _v4.Contains(mv);
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return IPAddressUtility.TryV4(ip, out var v) && _v4.Contains(v);
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return _v6.Contains(ip.ToUInt128());
        }

        return false;
    }
}
