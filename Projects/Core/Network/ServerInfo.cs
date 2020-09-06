/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: ServerInfo.cs                                                   *
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

namespace Server.Network
{
    public sealed class ServerInfo
    {
        public ServerInfo(string name, int fullPercent, TimeZoneInfo tz, IPEndPoint address)
        {
            Name = name;
            FullPercent = fullPercent;
            TimeZone = tz.GetUtcOffset(DateTime.Now).Hours;
            Address = address;
        }

        public string Name { get; set; }

        public int FullPercent { get; set; }

        public int TimeZone { get; set; }

        public IPEndPoint Address { get; set; }
    }
}
