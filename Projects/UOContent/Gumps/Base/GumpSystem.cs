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

using Server.Network;
using Server.Systems.FeatureFlags;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server.Gumps;

public static partial class GumpSystem
{
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

    private static void Remove(NetState ns, BaseGump gump)
    {
        if (_gumps.TryGetValue(ns, out var gumps))
        {
            for (var i = 0; i < gumps.Count; i++)
            {
                if (gumps[i] == gump)
                {
                    gumps.RemoveAt(i);
                    return;
                }
            }
        }
    }

    private static NetStateGumps Get(NetState ns)
    {
        if (ns == null)
        {
            return new NetStateGumps(null, null);
        }

        ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(_gumps, ns, out var exists);

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

        var state = m.NetState;
        return state != null && Get(state).Find<T>() != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FindGump<T>([DisallowNull] this Mobile m) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(m);

        var state = m.NetState;
        return state != null ? Get(state).Find<T>() : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CloseGump<T>([DisallowNull] this Mobile m) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(m);

        var state = m.NetState;
        return state != null && Get(state).Close<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendGump([DisallowNull] this Mobile m, BaseGump g, bool singleton = false)
    {
        ArgumentNullException.ThrowIfNull(m);

        if (m.AccessLevel < FeatureFlagSettings.RequiredAccessLevel
            && FeatureFlagManager.IsGumpBlocked(g.GetType()))
        {
            var entry = FeatureFlagManager.GetGumpBlockEntry(g.GetType());
            m.SendMessage(0x22, entry?.Reason ?? FeatureFlagSettings.DefaultGumpBlockedMessage);
            return;
        }

        var state = m.NetState;

        if (state != null)
        {
            Get(state).Send(g, singleton);
        }
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
        return Get(ns).Find<T>() != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FindGump<T>([DisallowNull] this NetState ns) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(ns);
        return Get(ns).Find<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CloseGump<T>([DisallowNull] this NetState ns) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(ns);
        return Get(ns).Close<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendGump([DisallowNull] this NetState ns, BaseGump g, bool singleton = false)
    {
        ArgumentNullException.ThrowIfNull(ns);
        Get(ns).Send(g, singleton);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NetStateGumps GetGumps([DisallowNull] this NetState ns)
    {
        ArgumentNullException.ThrowIfNull(ns);
        return Get(ns);
    }
}
