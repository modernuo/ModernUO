/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BinaryFileReader.cs                                             *
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
    public class BinaryFileReader : IGenericReader, IDisposable
    {
        private readonly BinaryReader _reader;

        public BinaryFileReader(BinaryReader br) => _reader = br;

        public BinaryFileReader(Stream stream) => _reader = new BinaryReader(stream);

        public void Close() => _reader.Close();

        public string ReadString(bool intern = false)
        {
            var str = _reader.ReadString();
            return intern ? Utility.Intern(str) : str;
        }

        public DateTime ReadDateTime() => new(_reader.ReadInt64(), DateTimeKind.Utc);

        public TimeSpan ReadTimeSpan() => new(_reader.ReadInt64());

        public DateTime ReadDeltaTime() => new(_reader.ReadInt64() + DateTime.UtcNow.Ticks, DateTimeKind.Utc);

        public decimal ReadDecimal() => _reader.ReadDecimal();

        public long ReadLong() => _reader.ReadInt64();

        public ulong ReadULong() => _reader.ReadUInt64();

        public int ReadInt() => _reader.ReadInt32();

        public uint ReadUInt() => _reader.ReadUInt32();

        public short ReadShort() => _reader.ReadInt16();

        public ushort ReadUShort() => _reader.ReadUInt16();

        public double ReadDouble() => _reader.ReadDouble();

        public float ReadFloat() => _reader.ReadSingle();

        public byte ReadByte() => _reader.ReadByte();

        public sbyte ReadSByte() => _reader.ReadSByte();

        public bool ReadBool() => _reader.ReadBoolean();

        public int ReadEncodedInt()
        {
            int v = 0, shift = 0;
            byte b;

            do
            {
                b = _reader.ReadByte();
                v |= (b & 0x7F) << shift;
                shift += 7;
            }
            while (b >= 0x80);

            return v;
        }

        public IPAddress ReadIPAddress()
        {
            byte length = ReadByte();
            // Either 2 ushorts, or 8 ushorts
            Span<byte> integer = stackalloc byte[length];
            Read(integer);
            return Utility.Intern(new IPAddress(integer));
        }

        public Point3D ReadPoint3D() => new(ReadInt(), ReadInt(), ReadInt());

        public Point2D ReadPoint2D() => new(ReadInt(), ReadInt());

        public Rectangle2D ReadRect2D() => new(ReadPoint2D(), ReadPoint2D());

        public Rectangle3D ReadRect3D() => new(ReadPoint3D(), ReadPoint3D());

        public Map ReadMap() => Map.Maps[ReadByte()];

        public Race ReadRace() => Race.Races[ReadByte()];

        public int Read(Span<byte> buffer) => _reader.Read(buffer);

        public long Seek(long offset, SeekOrigin origin) => _reader.BaseStream.Seek(offset, origin);

        public void Dispose() => Close();
    }
}
