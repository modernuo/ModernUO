/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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
using System.Runtime.InteropServices;
using System.Text;
using Server.Text;

namespace Server;

public class BufferWriter : IGenericWriter
{
    private readonly ConcurrentQueue<Type> _types;
    private readonly Encoding _encoding;
    private readonly bool _prefixStrings;

    // High-water mark for SeekOrigin.End. Writes advance _index directly and this is folded
    // in Seek/Resize (the only places the index can move backward), so the hot write path
    // carries no per-write bookkeeping.
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

    public virtual long Position => _index;

    protected virtual int BufferSize => 256;

    public byte[] Buffer => _buffer;

    public virtual void Close()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Resize(int size)
    {
        _bytesWritten = Math.Max(_bytesWritten, _index);

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

    public virtual void Flush() => Resize(Math.Clamp(_buffer.Length * 2, BufferSize, _buffer.Length + 1024 * 1024 * 64));

    /// <summary>
    /// Ensures capacity, returns a ref at the current position, and advances the index.
    /// The capacity check proves the caller's unaligned store is in-bounds, and the index
    /// only moves forward between Seek calls, so no per-write validation is needed. Growth
    /// (Flush -> Resize) always adds at least BufferSize, covering any primitive width.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte Reserve(int bytes)
    {
        if (_index + bytes > _buffer.Length)
        {
            Flush();
        }

        ref var result = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), (nint)_index);
        _index += bytes;
        return ref result;
    }

    public virtual void Write(byte[] bytes) => Write(bytes.AsSpan());

    public virtual void Write(byte[] bytes, int offset, int count) => Write(bytes.AsSpan(offset, count));

    public virtual void Write(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.Length;

        while (_buffer.Length - _index < length)
        {
            Flush();
        }

        bytes.CopyTo(_buffer.AsSpan((int)_index));
        _index += length;
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

        _bytesWritten = Math.Max(_bytesWritten, _index);

        return Index = Math.Max(0, origin switch
        {
            SeekOrigin.Current => _index + offset,
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
        if (!BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        Unsafe.WriteUnaligned(ref Reserve(8), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ulong value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        Unsafe.WriteUnaligned(ref Reserve(8), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        Unsafe.WriteUnaligned(ref Reserve(4), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        Unsafe.WriteUnaligned(ref Reserve(4), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(short value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        Unsafe.WriteUnaligned(ref Reserve(2), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ushort value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        Unsafe.WriteUnaligned(ref Reserve(2), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(double value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(BitConverter.DoubleToInt64Bits(value)));
        }

        Unsafe.WriteUnaligned(ref Reserve(8), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(float value)
    {
        if (!BitConverter.IsLittleEndian)
        {
            value = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(BitConverter.SingleToInt32Bits(value)));
        }

        Unsafe.WriteUnaligned(ref Reserve(4), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value) => Reserve(1) = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(sbyte value) => Reserve(1) = (byte)value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(bool value) => Reserve(1) = Unsafe.As<bool, byte>(ref value);

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
    public void Write(decimal value)
    {
        Span<int> buffer = stackalloc int[sizeof(decimal) / 4];
        decimal.GetBits(value, buffer);

        Write(MemoryMarshal.Cast<int, byte>(buffer));
    }

    // Class-level implementations of the hottest IGenericWriter default interface methods.
    // The JIT devirtualizes and inlines interface calls to the concrete type, but a default
    // interface method dispatches again internally on `this` for every nested Write — these
    // keep the whole write inlined instead.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEncodedInt(int value)
    {
        var v = (uint)value;

        while (v >= 0x80)
        {
            Write((byte)(v | 0x80));
            v >>= 7;
        }

        Write((byte)v);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(DateTime value)
    {
        // If DateTimeKind is Unspecified, we can't assume it needs to be converted.
        if (value.Kind == DateTimeKind.Local)
        {
            value = value.ToUniversalTime();
        }

        Write(value.Ticks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(TimeSpan value) => Write(value.Ticks);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Point3D value)
    {
        Write(value.m_X);
        Write(value.m_Y);
        Write(value.m_Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Point2D value)
    {
        Write(value.m_X);
        Write(value.m_Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Rectangle2D value)
    {
        Write(value.Start);
        Write(value.End);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Rectangle3D value)
    {
        Write(value.Start);
        Write(value.End);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Map value) => Write((byte)(value?.MapIndex ?? 0xFF));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Race value) => Write((byte)(value?.RaceIndex ?? 0xFF));

    internal void InternalWriteString(string value)
    {
        // Single pass for typical (short) strings: encode into a stack scratch, then write the
        // length prefix and copy. The two-pass path below walks the string twice
        // (GetByteCount + GetBytes) because the variable-width prefix must precede the bytes.
        // UTF8 needs at most 3 bytes per non-surrogate char (surrogate pairs encode 2 chars
        // into 4 bytes), so 85 chars always fit 255 bytes.
        if (value.Length <= 85)
        {
            Span<byte> scratch = stackalloc byte[256];
            var written = _encoding.GetBytes(value, scratch);

            WriteEncodedInt(written);

            while (_buffer.Length - _index < written)
            {
                Flush();
            }

            scratch[..written].CopyTo(_buffer.AsSpan((int)_index));
            _index += written;
            return;
        }

        var length = _encoding.GetByteCount(value);

        WriteEncodedInt(length);

        while (_buffer.Length - _index < length)
        {
            Flush();
        }

        // We don't use spans here since that incurs extra allocations for safety.
        _index += _encoding.GetBytes(value, 0, value.Length, _buffer, (int)_index);
    }
}
