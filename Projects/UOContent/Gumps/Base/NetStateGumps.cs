/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NetStateGumps.cs                                                *
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

public readonly ref struct NetStateGumps
{
    private const int GumpCap = 512;
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(NetStateGumps));

    private readonly List<BaseGump> _gumps;
    private readonly NetState _state;

    public NetStateGumps(List<BaseGump> gumps, NetState state)
    {
        _gumps = gumps;
        _state = state;
    }

    public bool Close<T>() where T : BaseGump
    {
        if (_state == null)
        {
            return false;
        }

        for (int i = 0; i < _gumps.Count; i++)
        {
            if (_gumps[i] is T tGump)
            {
                _state.SendCloseGump(tGump.TypeID, 0);
                tGump.OnServerClose(_state);

                _gumps.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public T Find<T>() where T : BaseGump
    {
        if (_state == null)
        {
            return null;
        }

        for (int i = 0; i < _gumps.Count; i++)
        {
            if (_gumps[i] is T tGump)
            {
                return tGump;
            }
        }

        return null;
    }

    public bool Has<T>() where T : BaseGump => Find<T>() != null;

    public void Send(BaseGump gump, bool singleton = false)
    {
        if (_state.CannotSendPackets()) // Cannot send packets handles _state null check
        {
            return;
        }

        if (singleton || gump.Singleton)
        {
            for (int i = 0; i < _gumps.Count; i++)
            {
                BaseGump old = _gumps[i];

                if (old.TypeID == gump.TypeID)
                {
                    _state.SendCloseGump(old.TypeID, 0);
                    old.OnServerClose(_state);

                    _gumps[i] = gump;
                    gump.SendTo(_state);
                    return;
                }
            }
        }

        if (_gumps.Count >= GumpCap)
        {
            _logger.Information("Exceeded gump cap, disconnecting...");
            _state.Disconnect("Exceeded gump cap.");
            return;
        }

        _gumps.Add(gump);
        gump.SendTo(_state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<BaseGump>.Enumerator GetEnumerator() =>
        ((ReadOnlySpan<BaseGump>)CollectionsMarshal.AsSpan(_gumps)).GetEnumerator();
}
