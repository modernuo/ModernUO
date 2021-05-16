/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IGenericReader.cs                                               *
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
using System.IO;
using System.Net;

namespace Server
{
    public interface IGenericReader
    {
        string ReadString(bool intern = false);
        long ReadLong();
        ulong ReadULong();
        int ReadInt();
        uint ReadUInt();
        short ReadShort();
        ushort ReadUShort();
        double ReadDouble();
        float ReadFloat();
        byte ReadByte();
        sbyte ReadSByte();
        bool ReadBool();

        DateTime ReadDateTime() => new(ReadLong(), DateTimeKind.Utc);
        TimeSpan ReadTimeSpan() => new(ReadLong());
        DateTime ReadDeltaTime() => new(ReadLong() + DateTime.UtcNow.Ticks, DateTimeKind.Utc);
        decimal ReadDecimal() => new(stackalloc int[4] { ReadInt(), ReadInt(), ReadInt(), ReadInt() });
        int ReadEncodedInt()
        {
            int v = 0, shift = 0;
            byte b;

            do
            {
                b = ReadByte();
                v |= (b & 0x7F) << shift;
                shift += 7;
            }
            while (b >= 0x80);

            return v;
        }
        IPAddress ReadIPAddress()
        {
            byte length = ReadByte();
            // Either 2 ushorts, or 8 ushorts
            Span<byte> integer = stackalloc byte[length];
            Read(integer);
            return Utility.Intern(new IPAddress(integer));
        }
        Point3D ReadPoint3D() => new(ReadInt(), ReadInt(), ReadInt());
        Point2D ReadPoint2D() => new(ReadInt(), ReadInt());
        Rectangle2D ReadRect2D() => new(ReadPoint2D(), ReadPoint2D());
        Rectangle3D ReadRect3D() => new(ReadPoint3D(), ReadPoint3D());
        Map ReadMap() => Map.Maps[ReadByte()];
        Race ReadRace() => Race.Races[ReadByte()];
        int Read(Span<byte> buffer);
        unsafe T ReadEnum<T>() where T : unmanaged, Enum
        {
            switch (ReadByte())
            {
                case 1:
                    {
                        var num = ReadByte();
                        return *(T*)&num;
                    }
                case 2:
                    {
                        var num = ReadShort();
                        return *(T*)&num;
                    }
                case 3:
                    {
                        var num = ReadEncodedInt();
                        return *(T*)&num;
                    }
                case 4:
                    {
                        var num = ReadLong();
                        return *(T*)&num;
                    }
            }

            return default;
        }
        long Seek(long offset, SeekOrigin origin);
    }
}
