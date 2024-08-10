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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Gumps;

public static partial class GumpSystem
{
    public const int GumpCap = 512;

    private static readonly Dictionary<NetState, List<BaseGump>> _gumps = [];

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

    private static T Find<T>(NetState ns) where T : BaseGump => ns != null ? Get(ns).Find<T>() : null;

    private static void Remove(NetState ns, BaseGump gump)
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

    private static void Send(NetState ns, BaseGump gump, bool singleton)
    {
        if (ns.CannotSendPackets()) // Handles ns null check too
        {
            return;
        }

        Get(ns).Send(gump, singleton);
    }

    private static bool Close<T>(NetState ns) where T : BaseGump => ns != null && Get(ns).Close<T>();

    private static NetStateGumps Get(NetState ns)
    {
        if (ns == null)
        {
            return new NetStateGumps(null, null);
        }

        ref List<BaseGump> list = ref CollectionsMarshal.GetValueRefOrAddDefault(_gumps, ns, out bool exists);

        if (!exists)
        {
            list = [];
        }

        return new NetStateGumps(list, ns);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasGump<T>([DisallowNull] this Mobile m) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(m);
        return Find<T>(m.NetState) != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FindGump<T>([DisallowNull] this Mobile m) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(m);
        return Find<T>(m.NetState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CloseGump<T>([DisallowNull] this Mobile m) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(m);
        return Close<T>(m.NetState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendGump([DisallowNull] this Mobile m, BaseGump g, bool singleton = false)
    {
        ArgumentNullException.ThrowIfNull(m);
        Send(m.NetState, g, singleton);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NetStateGumps GetGumps([DisallowNull] this Mobile m)
    {
        ArgumentNullException.ThrowIfNull(m);
        return Get(m.NetState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasGump<T>([DisallowNull] this NetState ns) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(ns);
        return Find<T>(ns) != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FindGump<T>([DisallowNull] this NetState ns) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(ns);
        return Find<T>(ns);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CloseGump<T>([DisallowNull] this NetState ns) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(ns);
        return Close<T>(ns);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendGump([DisallowNull] this NetState ns, BaseGump g, bool singleton = false)
    {
        ArgumentNullException.ThrowIfNull(ns);
        Send(ns, g, singleton);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NetStateGumps GetGumps([DisallowNull] this NetState ns)
    {
        ArgumentNullException.ThrowIfNull(ns);
        return Get(ns);
    }
}
