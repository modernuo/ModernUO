/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GameLoginEvent.cs                                               *
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
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server;

public class GameLoginEventArgs
{
    public GameLoginEventArgs(NetState state, string un, string pw)
    {
        State = state;
        Username = un;
        Password = pw;
    }

    public NetState State { get; }

    public string Username { get; }

    public string Password { get; }

    public bool Accepted { get; set; }

    public CityInfo[] CityInfo { get; set; }
}

public static partial class EventSink
{
    public static event Action<GameLoginEventArgs> GameLogin;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeGameLogin(GameLoginEventArgs e) => GameLogin?.Invoke(e);
}
