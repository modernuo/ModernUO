/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EventSink.cs                                                    *
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

namespace Server;

public static partial class EventSink
{
    public static event Action Shutdown;
    public static void InvokeShutdown() => Shutdown?.Invoke();

    public static event Action<Mobile> Logout;
    public static void InvokeLogout(Mobile m) => Logout?.Invoke(m);

    public static event Action<Mobile> Connected;
    public static void InvokeConnected(Mobile m) => Connected?.Invoke(m);

    public static event Action<Mobile> BeforeDisconnected;
    public static void InvokeBeforeDisconnected(Mobile m) => BeforeDisconnected?.Invoke(m);

    public static event Action<Mobile> Disconnected;
    public static void InvokeDisconnected(Mobile m) => Disconnected?.Invoke(m);

    public static event Action<Mobile, Mobile> PaperdollRequest;

    public static void InvokePaperdollRequest(Mobile beholder, Mobile beheld) =>
        PaperdollRequest?.Invoke(beholder, beheld);

    public static event Action ServerStarted;
    public static void InvokeServerStarted() => ServerStarted?.Invoke();
}
