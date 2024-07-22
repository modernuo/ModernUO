/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpSystem.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Logging;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Gumps;

public static partial class GumpSystem
{
    private const int GumpCap = 512;
    private const int InitialCapacity = 4;

    private static readonly Dictionary<NetState, List<BaseGump>> _gumps = [];
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(GumpSystem));

    public static unsafe void Configure()
    {
        IncomingPackets.Register(0xB1, 0, true, &DisplayGumpResponse);

        EventSink.Disconnected += EventSink_Disconnected;
    }

    private static void EventSink_Disconnected(Mobile m)
    {
        if (m.NetState != null && _gumps.Remove(m.NetState, out var gumps))
        {
            gumps.Clear();
        }
    }

    public static ReadOnlySpan<BaseGump> GetAllGumps(this NetState ns) =>
        _gumps.TryGetValue(ns, out var gumps) ? CollectionsMarshal.AsSpan(gumps) : [];

    public static T FindGump<T>(this NetState ns) where T : BaseGump
    {
        if (ns != null && _gumps.TryGetValue(ns, out var gumps))
        {
            for (int i = 0; i < gumps.Count; i++)
            {
                if (gumps[i] is T tGump)
                {
                    return tGump;
                }
            }
        }

        return null;
    }

    public static void AddGump(this NetState ns, BaseGump gump)
    {
        if (!_gumps.TryGetValue(ns, out var gumps))
        {
            gumps = new List<BaseGump>(InitialCapacity);
            _gumps.Add(ns, gumps);
        }

        if (gumps.Count < GumpCap)
        {
            gumps.Add(gump);
        }
        else
        {
            _logger.Information("Exceeded gump cap, disconnecting...");
            ns.Disconnect("Exceeded gump cap.");
        }
    }

    private static void RemoveGump(this NetState ns, BaseGump gump)
    {
        if (_gumps.TryGetValue(ns, out var gumps))
        {
            for (int i = 0; i < gumps.Count; i++)
            {
                if (gumps[i] == gump)
                {
                    gumps.RemoveAt(i);
                    return;
                }
            }
        }
    }

    private static bool RemoveGump<T>(this NetState ns, out T gump) where T : BaseGump
    {
        if (_gumps.TryGetValue(ns, out var gumps))
        {
            for (int i = 0; i < gumps.Count; i++)
            {
                if (gumps[i] is T tGump)
                {
                    gumps.RemoveAt(i);
                    gump = tGump;
                    return true;
                }
            }
        }

        gump = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasGump<T>(this Mobile m) where T : BaseGump => m.NetState.HasGump<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FindGump<T>(this Mobile m) where T : BaseGump => m.NetState.FindGump<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CloseGump<T>(this Mobile m) where T : BaseGump => m.NetState.CloseGump<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendGump(this Mobile m, BaseGump g) => m.NetState.SendGump(g);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasGump<T>(this NetState ns) where T : BaseGump => FindGump<T>(ns) != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendGump(this NetState ns, BaseGump g)
    {
        if (ns != null)
        {
            g.SendTo(ns);
        }
    }

    public static bool CloseGump<T>(this NetState ns) where T : BaseGump
    {
        if (ns != null && RemoveGump<T>(ns, out var gump))
        {
            ns.SendCloseGump(gump.TypeID, 0);
            gump.OnServerClose(ns);
            return true;
        }

        return false;
    }
}
