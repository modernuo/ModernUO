/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
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
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Server.Gumps;

public abstract class BaseGump
{
    private static readonly byte[] _packetBuffer = GC.AllocateUninitializedArray<byte>(0x10000);
    private static Serial nextSerial = (Serial)1;

    public int TypeID { get; protected set; }
    public Serial Serial { get; protected set; }

    public abstract int Switches { get; }
    public abstract int TextEntries { get; }

    public int X { get; set; }
    public int Y { get; set; }

    /**
     * If true, only one instance of this gump can be open at a time per player.
     */
    public virtual bool Singleton => false;

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

    public virtual void SendTo(NetState ns)
    {
        var writer = new SpanWriter(_packetBuffer);
        Compile(ref writer);

        ns.Send(writer.Span);

        writer.Dispose();
    }

    public abstract void Compile(ref SpanWriter writer);

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

    public static Point2D GetItemGraphicOffset(int itemId)
    {
        var (width, height) = ItemBounds.Sizes[itemId];
        var x = 0;
        var y = 0;

        if (width > 44)
        {
            x -= (width - 44) / 2;
        }
        else if (width < 44)
        {
            x += (44 - width) / 2;
        }

        if (height > 44)
        {
            y -= height - 44;
        }
        else if (height < 44)
        {
            y += 44 - height;
        }

        return new Point2D(x, y);
    }

    public static Point2D GetGumpOffsetForItemGraphic(int itemId, int relativeX, int relativeY)
    {
        var offset = GetItemGraphicOffset(itemId);
        return new Point2D(relativeX * 22 - relativeY * 22 + offset.X, relativeX * 22 + relativeY * 22 + offset.Y);
    }
}
