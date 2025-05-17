/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IFirewallEntry.cs                                               *
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

namespace Server.Network;

public interface IFirewallEntry : IComparable<IFirewallEntry>
{
    UInt128 MinIpAddress { get; }
    UInt128 MaxIpAddress { get; }

    int IComparable<IFirewallEntry>.CompareTo(IFirewallEntry? other)
    {
        if (other == null)
        {
            return 1;
        }

        if (MinIpAddress < other.MinIpAddress)
        {
            return -1;
        }

        if (MinIpAddress > other.MinIpAddress)
        {
            return 1;
        }

        if (MaxIpAddress > other.MaxIpAddress)
        {
            return -1;
        }

        if (MaxIpAddress < other.MaxIpAddress)
        {
            return 1;
        }

        return 0; // Equal ranges
    }

    bool IsBlocked(IPAddress address);

    bool IsBlocked(UInt128 address);
}
