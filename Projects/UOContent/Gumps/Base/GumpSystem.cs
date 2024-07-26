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

using Server.Gumps.Base;
using Server.Items;
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

    private static ReadOnlySpan<BaseGump> GetAll(NetState ns)
    {
        return _gumps.TryGetValue(ns, out var gumps) ? CollectionsMarshal.AsSpan(gumps) : [];
    }

    private static T Find<T>(NetState ns) where T : BaseGump
    {
        if (_gumps.TryGetValue(ns, out var gumps))
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

    private static void Add(NetState ns, BaseGump gump)
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

    private static bool Remove<T>(NetState ns, out T gump) where T : BaseGump
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

    private static void Send(NetState ns, BaseGump gump, bool singleton)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        ref List<BaseGump> list = ref CollectionsMarshal.GetValueRefOrAddDefault(_gumps, ns, out bool exists);

        if (exists)
        {
            bool replaced = false;

            if (singleton || gump.Singleton)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    BaseGump old = list[i];

                    if (old.TypeID == gump.TypeID)
                    {
                        ns.SendCloseGump(old.TypeID, 0);
                        old.OnServerClose(ns);

                        list[i] = gump;
                        replaced = true;
                        break;
                    }
                }
            }

            if (!replaced)
            {
                list.Add(gump);
            }
        }
        else
        {
            list = [gump];
        }

        gump.SendTo(ns);
    }

    private static bool Close<T>(NetState ns) where T : BaseGump
    {
        if (Remove<T>(ns, out var gump))
        {
            ns.SendCloseGump(gump.TypeID, 0);
            gump.OnServerClose(ns);
            return true;
        }

        return false;
    }

    private static MobileGumps Get(NetState ns)
    {
        ref List<BaseGump> list = ref CollectionsMarshal.GetValueRefOrAddDefault(_gumps, ns, out bool exists);

        if (!exists)
        {
            list = [];
        }

        return new MobileGumps(list, ns);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasGump<T>(this Mobile m) where T : BaseGump
    {
        return m.NetState is { } ns && Find<T>(ns) != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FindGump<T>(this Mobile m) where T : BaseGump
    {
        if (m.NetState is { } ns)
        {
            return Find<T>(ns);
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CloseGump<T>(this Mobile m) where T : BaseGump
    {
        return m.NetState is { } ns && Close<T>(ns);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendGump(this Mobile m, BaseGump g, bool singleton = false)
    {
        if (m.NetState is { } ns)
        {
            Send(ns, g, singleton);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<BaseGump> GetAllGumps(this Mobile m)
    {
        return m.NetState is { } ns ? GetAll(ns) : [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MobileGumps GetGumps(this Mobile m)
    {
        if (m.NetState is { } ns)
        {
            return Get(ns);
        }

        return new MobileGumps(null, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasGump<T>(this NetState ns) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(ns, nameof(ns));

        return Find<T>(ns) != null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendGump(this NetState ns, BaseGump g, bool singleton = false)
    {
        ArgumentNullException.ThrowIfNull(ns, nameof(ns));

        Send(ns, g, singleton);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CloseGump<T>(this NetState ns) where T : BaseGump
    {
        ArgumentNullException.ThrowIfNull(ns, nameof(ns));

        return Close<T>(ns);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<BaseGump> GetAllGumps(this NetState ns)
    {
        ArgumentNullException.ThrowIfNull(ns, nameof(ns));

        return GetAll(ns);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddGump(this NetState ns, BaseGump gump)
    {
        ArgumentNullException.ThrowIfNull(ns, nameof(ns));

        Add(ns, gump);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MobileGumps GetGumps(this NetState ns)
    {
        ArgumentNullException.ThrowIfNull(ns, nameof(ns));

        return Get(ns);
    }
}
