/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ServerListEvent.cs                                              *
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
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Network;

namespace Server;

public class ServerListEventArgs
{
    public ServerListEventArgs(NetState state, IAccount account)
    {
        State = state;
        Account = account;
        Servers = new List<ServerInfo>();
    }

    public NetState State { get; }

    public IAccount Account { get; }

    public bool Rejected { get; set; }

    public List<ServerInfo> Servers { get; }

    public void AddServer(string name, IPEndPoint address)
    {
        AddServer(name, 0, TimeZoneInfo.Local, address);
    }

    public void AddServer(string name, int fullPercent, TimeZoneInfo tz, IPEndPoint address)
    {
        Servers.Add(new ServerInfo(name, fullPercent, tz, address));
    }
}

public static partial class EventSink
{
    public static event Action<ServerListEventArgs> ServerList;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeServerList(ServerListEventArgs e) => ServerList?.Invoke(e);
}
