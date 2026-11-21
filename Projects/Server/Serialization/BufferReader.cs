/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Server.Text;

namespace Server;

/// <summary>
/// Reads little-endian serialized data from a byte array. Every read is bounds-checked, so
/// corrupt data throws <see cref="EndOfStreamException" /> instead of reading past the end.
/// </summary>
public class BufferReader : IGenericReader
{
    private readonly Dictionary<ulong, string> _typesDb;
    private readonly Encoding _encoding;
    private readonly byte[] _buffer;
    private int _position;

    public long Position => _position;
    public long BufferSize => _buffer.Length;

    public TimeSpan AnchoredTimeShift { get; set; }

    public DateTime AnchoredTimeOrigin { get; set; }

    public BufferReader(byte[] buffer, Dictionary<ulong, string> typesDb = null, Encoding encoding = null)
    {
        _buffer = buffer;
        _encoding = encoding ?? TextEncoding.UTF8;
        _typesDb = typesDb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref byte Take(int bytes)
    {
        var position = _position;

        if ((uint)bytes > (uint)(_buffer.Length - position))
        {
            ThrowEndOfData(bytes);
        }

        _position = position + bytes;
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> TakeSpan(int bytes) => MemoryMarshal.CreateReadOnlySpan(ref Take(bytes), bytes);

    [DoesNotReturn]
    private void ThrowEndOfData(int bytes) =>
        throw new EndOfStreamException(
            $"Reading {bytes} bytes at position {_position} passes the end of the data ({_buffer.Length} bytes)."
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(bool intern = false) => ReadBool() ? ReadStringRaw(intern) : null;

    public string ReadStringRaw(bool intern = false)
    {
        var length = ReadEncodedInt();

        if (length == 0)
        {
            return string.Empty;
        }

        var str = TextEncoding.GetString(TakeSpan(length), _encoding);
        return intern ? str.Intern() : str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => BinaryPrimitives.ReadInt64LittleEndian(TakeSpan(8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => BinaryPrimitives.ReadUInt64LittleEndian(TakeSpan(8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => BinaryPrimitives.ReadInt32LittleEndian(TakeSpan(4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => BinaryPrimitives.ReadUInt32LittleEndian(TakeSpan(4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => BinaryPrimitives.ReadInt16LittleEndian(TakeSpan(2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => BinaryPrimitives.ReadUInt16LittleEndian(TakeSpan(2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(TakeSpan(8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => BinaryPrimitives.ReadSingleLittleEndian(TakeSpan(4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => Take(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)Take(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => Take(1) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Serial ReadSerial() => (Serial)ReadUInt();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadEncodedInt()
    {
        var b = Take(1);
        return b < 0x80 ? b : ReadEncodedIntMultiByte(b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ReadEncodedIntMultiByte(byte first)
    {
        var v = first & 0x7F;

        for (var shift = 7; shift < 35; shift += 7)
        {
            var b = Take(1);
            v |= (b & 0x7F) << shift;

            if (b < 0x80)
            {
                return v;
            }
        }

        throw new InvalidDataException($"Encoded int at position {_position - 5} is longer than 5 bytes.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime ReadDateTime() => new(ReadLong(), DateTimeKind.Utc);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimeSpan ReadTimeSpan() => new(ReadLong());

    public DateTime ReadAnchoredTime() =>
        IGenericReader.RebaseAnchoredTime(ReadDateTime(), AnchoredTimeShift, AnchoredTimeOrigin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point3D ReadPoint3D()
    {
        var p = TakeSpan(12);
        return new Point3D(
            BinaryPrimitives.ReadInt32LittleEndian(p),
            BinaryPrimitives.ReadInt32LittleEndian(p[4..]),
            BinaryPrimitives.ReadInt32LittleEndian(p[8..])
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point2D ReadPoint2D()
    {
        var p = TakeSpan(8);
        return new Point2D(BinaryPrimitives.ReadInt32LittleEndian(p), BinaryPrimitives.ReadInt32LittleEndian(p[4..]));
    }

    public Rectangle2D ReadRect2D() => new(ReadPoint2D(), ReadPoint2D());

    public Rectangle3D ReadRect3D() => new(ReadPoint3D(), ReadPoint3D());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map ReadMap() => Map.Maps[Take(1)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Race ReadRace() => Race.Races[Take(1)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T ReadEnum<T>() where T : unmanaged, Enum
    {
        switch (sizeof(T))
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
            case 4:
                {
                    var num = ReadEncodedInt();
                    return *(T*)&num;
                }
            case 8:
                {
                    var num = ReadLong();
                    return *(T*)&num;
                }
        }

        return default;
    }

    public Type ReadType() =>
        ReadByte() switch
        {
            1 => AssemblyHandler.FindTypeByFullName(ReadStringRaw()), // Backward compatibility
            2 => ReadTypeByHash(),
            _ => null
        };

    public Type ReadTypeByHash() => SerializedTypeResolver.Resolve(ReadULong(), _typesDb, _position);

    public int Read(Span<byte> buffer)
    {
        TakeSpan(buffer.Length).CopyTo(buffer);
        return buffer.Length;
    }

    public virtual long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End     => _buffer.Length + offset,
            _                  => offset // Begin
        };

        if ((ulong)position > (ulong)_buffer.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                $"Seeking to {position} is outside the data (0 to {_buffer.Length})."
            );
        }

        _position = (int)position;
        return position;
    }
}
