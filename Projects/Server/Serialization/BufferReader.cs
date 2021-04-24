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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Guilds;
using Server.Network;
using Server.Text;

namespace Server
{
    public class BufferReader : IGenericReader
    {
        private readonly Encoding _encoding;
        private byte[] _buffer;
        private int _position;

        public int Position => _position;

        public byte[] Buffer => _buffer;

        public BufferReader(byte[] buffer)
        {
            _buffer = buffer;
            _encoding = TextEncoding.UTF8;
        }

        public void SwapBuffers(byte[] newBuffer, out byte[] oldBuffer)
        {
            oldBuffer = _buffer;
            _buffer = newBuffer;
            _position = 0;
        }

        [Obsolete("RunUO backward compatible method that should be replaced.")]
        public string ReadString()
        {
            if (!ReadBool())
            {
                return null;
            }

            var length = ReadEncodedInt();
            return length <= 0 ? "" : ReadString(_encoding, false, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString(Encoding encoding, bool safeString = false, int fixedLength = -1)
        {
            int sizeT = TextEncoding.GetByteLengthForEncoding(encoding);

            bool isFixedLength = fixedLength > -1;

            var remaining = _buffer.Length - _position;
            int size;

            if (isFixedLength)
            {
                size = fixedLength * sizeT;
                if (size > remaining)
                {
                    throw new OutOfMemoryException();
                }
            }
            else
            {
                size = remaining - (remaining & (sizeT - 1));
            }

            var buffer = _buffer.AsSpan(Position, size);

            int index = buffer.IndexOfTerminator(sizeT);

            var span = buffer[..(index < 0 ? size : index)];
            _position += isFixedLength || index < 0 ? size : index + sizeT;
            return TextEncoding.GetString(span, encoding, safeString);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUniSafe(int fixedLength) => ReadString(TextEncoding.UnicodeLE, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUniSafe() => ReadString(TextEncoding.UnicodeLE, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUni(int fixedLength) => ReadString(TextEncoding.UnicodeLE, false, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUni() => ReadString(TextEncoding.UnicodeLE);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUniSafe(int fixedLength) => ReadString(TextEncoding.Unicode, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUniSafe() => ReadString(TextEncoding.Unicode, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUni(int fixedLength) => ReadString(TextEncoding.Unicode, false, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUni() => ReadString(TextEncoding.Unicode);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8Safe(int fixedLength) => ReadString(TextEncoding.UTF8, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8Safe() => ReadString(TextEncoding.UTF8, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8() => ReadString(TextEncoding.UTF8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAsciiSafe(int fixedLength) => ReadString(Encoding.ASCII, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAsciiSafe() => ReadString(Encoding.ASCII, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAscii(int fixedLength) => ReadString(Encoding.ASCII, false, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAscii() => ReadString(Encoding.ASCII);

        public DateTime ReadDateTime() => new(ReadLong(), DateTimeKind.Utc);

        public TimeSpan ReadTimeSpan() => new(ReadLong());

        public DateTime ReadDeltaTime() => new(ReadLong() + DateTime.UtcNow.Ticks, DateTimeKind.Utc);

        public decimal ReadDecimal() => new(new[] { ReadInt(), ReadInt(), ReadInt(), ReadInt() });

        public long ReadLong()
        {
            var v = BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(Position, 8));
            _position += 8;
            return v;
        }

        public ulong ReadULong()
        {
            var v = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(Position, 8));
            _position += 8;
            return v;
        }

        public int ReadInt()
        {
            var v = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(Position, 4));
            _position += 4;
            return v;
        }

        public uint ReadUInt()
        {
            var v = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(Position, 4));
            _position += 4;
            return v;
        }

        public short ReadShort()
        {
            var v = BinaryPrimitives.ReadInt16LittleEndian(_buffer.AsSpan(Position, 2));
            _position += 2;
            return v;
        }

        public ushort ReadUShort()
        {
            var v = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(Position, 2));
            _position += 2;
            return v;
        }

        public double ReadDouble()
        {
            var v = BinaryPrimitives.ReadDoubleLittleEndian(_buffer.AsSpan(Position, 8));
            _position += 8;
            return v;
        }

        public float ReadFloat()
        {
            var v = BinaryPrimitives.ReadSingleLittleEndian(_buffer.AsSpan(Position, 4));
            _position += 4;
            return v;
        }

        public byte ReadByte() => _buffer[_position++];

        public sbyte ReadSByte() => (sbyte)_buffer[_position++];

        public bool ReadBool() => _buffer[_position++] != 0;

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
            _position += length;
            return length;
        }

        public virtual int Seek(int offset, SeekOrigin origin)
        {
            Debug.Assert(
                origin != SeekOrigin.End || offset <= 0 && offset > -_buffer.Length,
                "Attempting to seek to an invalid position using SeekOrigin.End"
            );
            Debug.Assert(
                origin != SeekOrigin.Begin || offset >= 0 && offset < _buffer.Length,
                "Attempting to seek to an invalid position using SeekOrigin.Begin"
            );
            Debug.Assert(
                origin != SeekOrigin.Current || _position + offset >= 0 && _position + offset < _buffer.Length,
                "Attempting to seek to an invalid position using SeekOrigin.Current"
            );

            return _position = Math.Max(0, origin switch
            {
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End     => _buffer.Length + offset,
                _                  => offset // Begin
            });
        }
    }
}
