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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Collections;
using Server.Text;

namespace Server
{
    public class BufferReader : IGenericReader
    {
        private readonly Encoding _encoding;
        private byte[] _buffer;
        private int _position;

        public long Position => _position;

        public BufferReader(byte[] buffer, Encoding encoding = null)
        {
            _buffer = buffer;
            _encoding = encoding ?? TextEncoding.UTF8;
        }

        public BufferReader(byte[] buffer, DateTime lastSerialized) : this(buffer) => LastSerialized = lastSerialized;

        public void Reset(byte[] newBuffer, out byte[] oldBuffer)
        {
            oldBuffer = _buffer;
            _buffer = newBuffer;
            _position = 0;
        }

        // Compatible with BinaryReader.ReadString()
        public DateTime LastSerialized { get; init; }

        public string ReadString(bool intern = false)
        {
            if (!ReadBool())
            {
                return null;
            }

            var length = ((IGenericReader)this).ReadEncodedInt();
            if (length <= 0)
            {
                return intern ? Utility.Intern("") : "";
            }

            var str = TextEncoding.GetString(_buffer.AsSpan(_position, length), _encoding);
            _position += length;
            return intern ? Utility.Intern(str) : str;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadLong()
        {
            var v = BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(_position, 8));
            _position += 8;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadULong()
        {
            var v = BinaryPrimitives.ReadUInt64LittleEndian(_buffer.AsSpan(_position, 8));
            _position += 8;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt()
        {
            var v = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_position, 4));
            _position += 4;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt()
        {
            var v = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(_position, 4));
            _position += 4;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadShort()
        {
            var v = BinaryPrimitives.ReadInt16LittleEndian(_buffer.AsSpan(_position, 2));
            _position += 2;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUShort()
        {
            var v = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(_position, 2));
            _position += 2;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            var v = BinaryPrimitives.ReadDoubleLittleEndian(_buffer.AsSpan(_position, 8));
            _position += 8;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat()
        {
            var v = BinaryPrimitives.ReadSingleLittleEndian(_buffer.AsSpan(_position, 4));
            _position += 4;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() => _buffer[_position++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte() => (sbyte)_buffer[_position++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool() => _buffer[_position++] != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Serial ReadSerial() => (Serial)ReadUInt();

        public int Read(Span<byte> buffer)
        {
            var length = buffer.Length;
            if (length > _buffer.Length - _position)
            {
                throw new OutOfMemoryException();
            }

            _buffer.AsSpan(_position, length).CopyTo(buffer);
            _position += length;
            return length;
        }

        public BitArray ReadBitArray()
        {
            var bitLength = ((IGenericReader)this).ReadEncodedInt();
            var length = BitArray.GetByteArrayLengthFromBitLength(bitLength);

            if (length > _buffer.Length - _position)
            {
                throw new OutOfMemoryException();
            }

            var bitArray = new BitArray(_buffer.AsSpan(_position, length), bitLength);
            _position += length;
            return bitArray;
        }

        public virtual long Seek(long offset, SeekOrigin origin)
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

            var position = Math.Max(0L, origin switch
            {
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End     => _buffer.Length + offset,
                _                  => offset // Begin
            });

            if (position > int.MaxValue)
            {
                throw new ArgumentException($"BufferReader does not support {nameof(offset)} beyond Int32.MaxValue");
            }

            _position = (int)position;

            return _position;
        }
    }
}
