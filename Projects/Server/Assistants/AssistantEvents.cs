/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AssistantEvents.cs                                              *
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
using Server.Accounting;
using Server.Network;

namespace Server;

public class AssistantAuthEventArgs
{
    public NetState State { get; }
    public IAccount Account { get; }
    public bool AuthOk { get; }

    public AssistantAuthEventArgs(NetState state, IAccount acct, bool authOK)
    {
        State = state;
        Account = acct;
        AuthOk = authOK;
    }
}

public static partial class EventSink
{
    public static event Action<AssistantAuthEventArgs> AssistantAuth;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeAssistantAuth(AssistantAuthEventArgs e) => AssistantAuth?.Invoke(e);
}
