/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BufferReader.cs                                                 *
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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Server.Guilds;

namespace Server
{
    public class BufferReader : IGenericReader
    {
        private readonly Encoding _encoding;
        private byte[] _buffer;
        public int Position { get; private set; }

        public BufferReader(byte[] buffer)
        {
            _buffer = buffer;
            _encoding = Utility.UTF8;
        }

        public string ReadString()
        {
            if (!ReadBool())
            {
                return null;
            }

            var length = ReadEncodedInt();
            if (length <= 0)
            {
                return "";
            }

            var s = _encoding.GetString(_buffer.AsSpan(Position, length));
            Position += length;
            return s;
        }

        public DateTime ReadDateTime() => new(ReadLong(), DateTimeKind.Utc);

        public TimeSpan ReadTimeSpan() => new(ReadLong());

        public DateTime ReadDeltaTime() => new(ReadLong() + DateTime.UtcNow.Ticks, DateTimeKind.Utc);

        public decimal ReadDecimal() => new(new[] { ReadInt(), ReadInt(), ReadInt(), ReadInt() });

        public long ReadLong()
        {
            var v = BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(Position, 8));
            Position += 8;
            return v;
        }

        public ulong ReadULong()
        {
            var v = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(Position, 8));
            Position += 8;
            return v;
        }

        public int ReadInt()
        {
            var v = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(Position, 4));
            Position += 4;
            return v;
        }

        public uint ReadUInt()
        {
            var v = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(Position, 4));
            Position += 4;
            return v;
        }

        public short ReadShort()
        {
            var v = BinaryPrimitives.ReadInt16LittleEndian(_buffer.AsSpan(Position, 2));
            Position += 2;
            return v;
        }

        public ushort ReadUShort()
        {
            var v = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(Position, 2));
            Position += 2;
            return v;
        }

        public double ReadDouble()
        {
            var v = BinaryPrimitives.ReadDoubleLittleEndian(_buffer.AsSpan(Position, 8));
            Position += 8;
            return v;
        }

        public float ReadFloat()
        {
            var v = BinaryPrimitives.ReadSingleLittleEndian(_buffer.AsSpan(Position, 4));
            Position += 4;
            return v;
        }

        public byte ReadByte() => _buffer[Position++];

        public sbyte ReadSByte() => (sbyte)_buffer[Position++];

        public bool ReadBool() => _buffer[Position++] != 0;

        public int ReadEncodedInt()
        {
            int v = 0, shift = 0;
            byte b;

            do
            {
                b = ReadByte();
                v |= (b & 0x7F) << shift;
                shift += 7;
            } while (b >= 0x80);

            return v;
        }

        public IPAddress ReadIPAddress()
        {
            byte length = ReadByte();
            // Either 2 ushorts, or 8 ushorts
            Span<byte> integer = stackalloc byte[length];
            Read(integer);
            return new IPAddress(integer);
        }

        public Point3D ReadPoint3D() => new(ReadInt(), ReadInt(), ReadInt());

        public Point2D ReadPoint2D() => new(ReadInt(), ReadInt());

        public Rectangle2D ReadRect2D() => new(ReadPoint2D(), ReadPoint2D());

        public Rectangle3D ReadRect3D() => new(ReadPoint3D(), ReadPoint3D());

        public Map ReadMap() => Map.Maps[ReadByte()];

        public T ReadEntity<T>() where T : class, ISerializable
        {
            Serial serial = ReadUInt();

            // Special case for now:
            if (typeof(T).IsAssignableTo(typeof(BaseGuild)))
            {
                return World.FindGuild(serial) as T;
            }

            return World.FindEntity(serial) as T;
        }

        public List<T> ReadEntityList<T>() where T : class, ISerializable
        {
            var count = ReadInt();

            var list = new List<T>(count);

            for (var i = 0; i < count; ++i)
            {
                var entity = ReadEntity<T>();
                if (entity != null)
                {
                    list.Add(entity);
                }
            }

            return list;
        }

        public HashSet<T> ReadEntitySet<T>() where T : class, ISerializable
        {
            var count = ReadInt();

            var set = new HashSet<T>(count);

            for (var i = 0; i < count; ++i)
            {
                var entity = ReadEntity<T>();
                if (entity != null)
                {
                    set.Add(entity);
                }
            }

            return set;
        }

        public Race ReadRace() => Race.Races[ReadByte()];

        public int Read(Span<byte> buffer)
        {
            var length = buffer.Length;
            if (length > _buffer.Length - Position)
            {
                throw new OutOfMemoryException();
            }

            _buffer.AsSpan(Position, length).CopyTo(buffer);
            Position += length;
            return length;
        }

        public virtual int Seek(int offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Current => Position += offset,
                SeekOrigin.End     => Position = _buffer.Length - offset,
                _                  => Position = offset // Begin
            };
        }
    }
}
