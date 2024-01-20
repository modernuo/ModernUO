/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseFirewallEntry.cs                                            *
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
using System.Runtime.CompilerServices;

namespace Server.Network;

public abstract class BaseFirewallEntry : IFirewallEntry, ISpanFormattable
{
    public abstract UInt128 MinIpAddress { get; }
    public abstract UInt128 MaxIpAddress { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBlocked(IPAddress address) => IsBlocked(address.ToUInt128());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBlocked(UInt128 address) => address >= MinIpAddress && address <= MaxIpAddress;

    public override string ToString() =>
        MinIpAddress == MaxIpAddress ? MinIpAddress.ToIpAddress().ToString()
            : $"{MinIpAddress.ToIpAddress()}-{MaxIpAddress.ToIpAddress()}";

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        // format and provider are explicitly ignored
        ToString();

    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        if (!((ISpanFormattable)MinIpAddress.ToIpAddress()).TryFormat(destination, out charsWritten, format, provider))
        {
            return false;
        }

        if (MinIpAddress == MaxIpAddress)
        {
            return true;
        }

        // Range
        destination[charsWritten++] = '-';

        var total = charsWritten;

        if (!((ISpanFormattable)MaxIpAddress.ToIpAddress()).TryFormat(destination[charsWritten..], out charsWritten, format, provider))
        {
            return false;
        }

        charsWritten += total;
        return true;
    }
}
