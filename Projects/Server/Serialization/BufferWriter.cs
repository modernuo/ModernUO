/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BufferWriter.cs                                                 *
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Text;

namespace Server;

public class BufferWriter : IGenericWriter
{
    private ConcurrentQueue<Type> _types;
    private Encoding _encoding;
    private bool _prefixStrings;
    private long _bytesWritten;
    private long _index;

    protected long Index
    {
        get => _index;
        set
        {
            if (value < 0 || value > _buffer.Length)
            {
                // If you are receiving this exception and your value is too large, you may need to use `Resize`
                // If you are receiving this exception and your value is negative, you probably used Seek incorrectly.
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            _index = value;

            if (value > _bytesWritten)
            {
                _bytesWritten = value;
            }
        }
    }

    private byte[] _buffer;

    public BufferWriter(byte[] buffer, bool prefixStr, ConcurrentQueue<Type> types = null)
    {
        _prefixStrings = prefixStr;
        _encoding = TextEncoding.UTF8;
        _buffer = buffer;
        _types = types;
    }

    public BufferWriter(bool prefixStr, ConcurrentQueue<Type> types = null) : this(0, prefixStr, types)
    {
    }

    public BufferWriter(int count, bool prefixStr, ConcurrentQueue<Type> types = null)
    {
        _prefixStrings = prefixStr;
        _encoding = TextEncoding.UTF8;
        _buffer = GC.AllocateUninitializedArray<byte>(count < 1 ? BufferSize : count);
        _types = types;
    }

    public virtual long Position => Index;

    protected virtual int BufferSize => 256;

    public byte[] Buffer => _buffer;

    public virtual void Close()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Resize(int size)
    {
        // We shouldn't ever resize to a 0 length buffer. That is dangerous
        if (size <= 0)
        {
            size = BufferSize;
        }

        if (size < _buffer.Length)
        {
            _bytesWritten = size;
        }

        var newBuffer = GC.AllocateUninitializedArray<byte>(size);
        _buffer.AsSpan(0, Math.Min(size, _buffer.Length)).CopyTo(newBuffer);
        _buffer = newBuffer;
    }

    public virtual void Flush()
    {
        // Need to avoid buffer.Length = 2, buffer * 2 is 4, but we need 8 or 16bytes, causing an exception.
        // The least we need is 16bytes + Index, but we use BufferSize since it should always be big enough for a single
        // non-dynamic field.
        Resize(Math.Max(BufferSize, _buffer.Length * 2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FlushIfNeeded(int amount)
    {
        if (Index + amount > _buffer.Length)
        {
            Flush();
        }
    }

    public void Write(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.Length;

        while (_buffer.Length - _index < length)
        {
            Flush();
        }

        bytes.CopyTo(_buffer.AsSpan((int)_index));
        Index += length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            origin != SeekOrigin.Current || Index + offset >= 0 && Index + offset < _buffer.Length,
            "Attempting to seek to an invalid position using SeekOrigin.Current"
        );

        return Index = Math.Max(0, origin switch
        {
            SeekOrigin.Current => Index + offset,
            SeekOrigin.End     => _bytesWritten + offset,
            _                  => offset // Begin
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(string value)
    {
        if (_prefixStrings)
        {
            if (value == null)
            {
                Write(false);
            }
            else
            {
                Write(true);
                InternalWriteString(value);
            }
        }
        else
        {
            InternalWriteString(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(long value)
    {
        FlushIfNeeded(8);

        BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan((int)_index), value);
        Index += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ulong value)
    {
        FlushIfNeeded(8);

        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan((int)_index), value);
        Index += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
        FlushIfNeeded(4);

        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan((int)_index), value);
        Index += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
        FlushIfNeeded(4);

        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan((int)_index), value);
        Index += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(short value)
    {
        FlushIfNeeded(2);

        BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan((int)_index), value);
        Index += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ushort value)
    {
        FlushIfNeeded(2);

        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan((int)_index), value);
        Index += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(double value)
    {
        FlushIfNeeded(8);

        BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan((int)_index), value);
        Index += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(float value)
    {
        FlushIfNeeded(4);

        BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan((int)_index), value);
        Index += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value)
    {
        FlushIfNeeded(1);
        _buffer[Index++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(sbyte value)
    {
        FlushIfNeeded(1);
        _buffer[Index++] = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Write(bool value)
    {
        FlushIfNeeded(1);
        _buffer[Index++] = *(byte*)&value; // up to 30% faster to dereference the raw value on the stack
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Serial serial) => Write(serial.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Type type)
    {
        if (type == null)
        {
            Write((byte)0);
        }
        else
        {
            Write((byte)0x2); // xxHash3 64bit
            Write(AssemblyHandler.GetTypeHash(type));
            _types?.Enqueue(type);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void InternalWriteString(string value)
    {
        var length = _encoding.GetByteCount(value);

        ((IGenericWriter)this).WriteEncodedInt(length);

        while (_buffer.Length - _index < length)
        {
            Flush();
        }

        // We don't use spans here since that incurs extra allocations for safety.
        Index += _encoding.GetBytes(value, 0, value.Length, _buffer, (int)_index);
    }
}
