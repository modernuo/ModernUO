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
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        if ((uint)(_index + bytes) > (uint)_buffer.Length)
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
            origin != SeekOrigin.Current || _index + offset >= 0 && _index + offset < _buffer.Length,
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
                WriteRaw(value);
            }
        }
        else
        {
            WriteRaw(value);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEncodedInt(int value)
    {
        var v = (uint)value;

        // FAST PATH: 1 byte (0 to 127).
        // This keeps the inlined code incredibly tiny at the call site.
        if (v < 0x80)
        {
            Reserve(1) = (byte)v;
        }
        else
        {
            // SLOW PATH: Push to a non-inlined method to prevent code bloat.
            WriteEncodedIntMultiByte(v);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WriteEncodedIntMultiByte(uint v)
    {
        // We already know v >= 0x80. Unroll the loop entirely based on magnitude.
        // This allows us to call Reserve() exactly ONE time.

        if (v < 0x4000) // 2 bytes
        {
            ref byte ptr = ref Reserve(2);
            ptr = (byte)(v | 0x80);
            Unsafe.Add(ref ptr, 1) = (byte)(v >> 7);
        }
        else if (v < 0x200000) // 3 bytes
        {
            ref byte ptr = ref Reserve(3);
            ptr = (byte)(v | 0x80);
            Unsafe.Add(ref ptr, 1) = (byte)((v >> 7) | 0x80);
            Unsafe.Add(ref ptr, 2) = (byte)(v >> 14);
        }
        else if (v < 0x10000000) // 4 bytes
        {
            ref byte ptr = ref Reserve(4);
            ptr = (byte)(v | 0x80);
            Unsafe.Add(ref ptr, 1) = (byte)((v >> 7) | 0x80);
            Unsafe.Add(ref ptr, 2) = (byte)((v >> 14) | 0x80);
            Unsafe.Add(ref ptr, 3) = (byte)(v >> 21);
        }
        else // 5 bytes (including all negative numbers due to logical shift)
        {
            ref byte ptr = ref Reserve(5);
            ptr = (byte)(v | 0x80);
            Unsafe.Add(ref ptr, 1) = (byte)((v >> 7) | 0x80);
            Unsafe.Add(ref ptr, 2) = (byte)((v >> 14) | 0x80);
            Unsafe.Add(ref ptr, 3) = (byte)((v >> 21) | 0x80);
            Unsafe.Add(ref ptr, 4) = (byte)(v >> 28);
        }
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
    public void WriteDeltaTime(DateTime value)
    {
        if (value == DateTime.MinValue)
        {
            Write(long.MinValue);
            return;
        }

        if (value == DateTime.MaxValue)
        {
            Write(long.MaxValue);
            return;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            value = value.ToUniversalTime();
        }

        // Technically supports negative deltas for times in the past
        Write(value.Ticks - DateTime.UtcNow.Ticks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(IPAddress value)
    {
        Span<byte> stack = stackalloc byte[16];
        value.TryWriteBytes(stack, out var bytesWritten);
        Write((byte)bytesWritten);
        Write(stack[..bytesWritten]);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void WriteEnum<T>(T value) where T : unmanaged, Enum
    {
        switch (sizeof(T))
        {
            default:
                {
                    throw new ArgumentException($"Argument of type {typeof(T)} is not a normal enum");
                }
            case 1:
                {
                    Write(*(byte*)&value);
                    break;
                }
            case 2:
                {
                    Write(*(ushort*)&value);
                    break;
                }
            case 4:
                {
                    WriteEncodedInt(*(int*)&value);
                    break;
                }
            case 8:
                {
                    Write(*(ulong*)&value);
                    break;
                }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Guid guid)
    {
        Span<byte> stack = stackalloc byte[16];
        guid.TryWriteBytes(stack);
        Write(stack);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(BitArray bitArray)
    {
        var bitLength = bitArray.Length;
        var byteLength = (bitLength + 7) / 8;

        WriteEncodedInt(bitLength);

        var arrayBuffer = ArrayPool<byte>.Shared.Rent(byteLength);
        try
        {
            bitArray.CopyTo(arrayBuffer, 0);
            Write(arrayBuffer.AsSpan(0, byteLength));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(arrayBuffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(TextDefinition def)
    {
        if (def == null)
        {
            WriteEncodedInt(3);
        }
        else if (def.Number > 0)
        {
            WriteEncodedInt(1);
            WriteEncodedInt(def.Number);
        }
        else if (def.String != null)
        {
            WriteEncodedInt(2);
            Write(def.String);
        }
        else
        {
            WriteEncodedInt(0); // Empty
        }
    }

    public void WriteRaw(string value)
    {
        // Single pass, in place: reserve the UTF-8 worst case (3 bytes per char) plus a
        // length prefix sized for that worst case, encode directly into the buffer, then
        // write the actual byte count into the reserved prefix zero-padded to the same
        // width. Readers accumulate 7-bit groups, so non-minimal prefixes decode
        // identically — no second pass over the string, no scratch copy, no pooling.
        var maxLength = value.Length * 3;
        var prefixWidth = EncodedIntWidth(maxLength);

        while (_buffer.Length - _index < prefixWidth + maxLength)
        {
            Flush();
        }

        var written = _encoding.GetBytes(value, _buffer.AsSpan((int)(_index + prefixWidth)));

        WriteEncodedIntPadded(written, prefixWidth);
        _index += written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EncodedIntWidth(int value) =>
        value < 0x80 ? 1 : value < 0x4000 ? 2 : value < 0x20_0000 ? 3 : value < 0x1000_0000 ? 4 : 5;

    private void WriteEncodedIntPadded(int value, int width)
    {
        var v = (uint)value;

        for (var i = 1; i < width; i++)
        {
            _buffer[_index++] = (byte)(v | 0x80);
            v >>= 7;
        }

        _buffer[_index++] = (byte)v; // fits in 7 bits because width >= EncodedIntWidth(value)
    }
}
