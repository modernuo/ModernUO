/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BaseGump.cs                                                     *
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
using System;
using System.Runtime.CompilerServices;

namespace Server.Gumps;

public abstract class BaseGump
{
    private static Serial nextSerial = (Serial)1;

    public int TypeID { get; protected set; }
    public Serial Serial { get; protected set; }

    public abstract int Switches { get; }
    public abstract int TextEntries { get; }

    public int X { get; set; }

    public int Y { get; set; }

    public BaseGump(int x, int y) : this()
    {
        X = x;
        Y = y;
    }

    public BaseGump()
    {
        Serial = nextSerial++;
        TypeID = GetTypeId(GetType());
    }

    public abstract void SendTo(NetState ns);

    public virtual void OnResponse(NetState sender, in RelayInfo info)
    {
    }

    public virtual void OnServerClose(NetState owner)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetTypeId(Type type)
    {
        unchecked
        {
            // To use the original .NET Framework deterministic hash code (with really terrible performance)
            // change the next line to use HashUtility.GetNetFrameworkHashCode
            var hash = (int)HashUtility.ComputeHash32(type?.FullName);

            const int primeMulti = 0x108B76F1;

            // Virtue Gump
            return hash == 461 ? hash * primeMulti : hash;
        }
    }
}
