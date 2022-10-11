/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using System.Buffers.Binary;
using System.Net;

namespace Server.Network;

public sealed class ServerInfo
{
    private readonly IPEndPoint m_Address;

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

    public IPEndPoint Address
    {
        get => m_Address;
        init
        {
            m_Address = value;
            Span<byte> integer = stackalloc byte[4];
            value.Address.MapToIPv4().TryWriteBytes(integer, out var bytesWritten);
            if (bytesWritten != 4)
            {
                throw new InvalidOperationException("IP Address could not be serialized to an integer");
            }

            RawAddress = BinaryPrimitives.ReadUInt32LittleEndian(integer);
        }
    }

    // UO doesn't support IPv6 servers
    public uint RawAddress { get; private set; }
}
