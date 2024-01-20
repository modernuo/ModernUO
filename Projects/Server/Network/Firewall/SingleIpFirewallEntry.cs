/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SingleIpFirewallEntry.cs                                        *
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

public class SingleIpFirewallEntry : BaseFirewallEntry
{
    public override UInt128 MinIpAddress { get; }

    public override UInt128 MaxIpAddress => MinIpAddress;

    public SingleIpFirewallEntry(string ipAddress) => MinIpAddress = IPAddress.Parse(ipAddress).ToUInt128();

    public SingleIpFirewallEntry(IPAddress ipAddress) => MinIpAddress = ipAddress.ToUInt128();
}
