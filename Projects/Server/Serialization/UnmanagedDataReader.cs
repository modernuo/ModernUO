/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: UnmanagedDataReader.cs                                          *
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
using System.Text;
using Server.Text;

namespace Server;

/// <summary>
/// Reads little-endian serialized data from unmanaged memory, such as a mapped file view.
/// Every read is bounds-checked against the size given at construction, so corrupt data
/// throws <see cref="EndOfStreamException" /> instead of reading past the memory.
/// </summary>
public unsafe class UnmanagedDataReader : IGenericReader
{
    private readonly byte* _ptr;
    private readonly long _size;
    private long _position;

    private readonly Dictionary<ulong, string> _typesDb;
    private readonly Encoding _encoding;

    public long Position => _position;

    public TimeSpan AnchoredTimeShift { get; set; }

    public DateTime AnchoredTimeOrigin { get; set; }

    /// <param name="ptr">The start of the data.</param>
    /// <param name="size">The number of readable bytes from <paramref name="ptr" />.</param>
    /// <param name="typesDb">Legacy type database, needed to resolve hashes of renamed types.</param>
    /// <param name="encoding"><see cref="TextEncoding.UTF8" /> by default.</param>
    public UnmanagedDataReader(byte* ptr, long size, Dictionary<ulong, string> typesDb = null, Encoding encoding = null)
    {
        _encoding = encoding ?? TextEncoding.UTF8;
        _typesDb = typesDb;
        _ptr = ptr;
        _size = size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte* Take(int bytes)
    {
        var position = _position;

        if ((ulong)bytes > (ulong)(_size - position))
        {
            ThrowEndOfData(bytes);
        }

        _position = position + bytes;
        return _ptr + position;
    }

    [DoesNotReturn]
    private void ThrowEndOfData(int bytes) =>
        throw new EndOfStreamException($"Reading {bytes} bytes at position {_position} passes the end of the data ({_size} bytes).");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(bool intern = false) => ReadBool() ? ReadStringRaw(intern) : null;

    public string ReadStringRaw(bool intern = false)
    {
        var length = ReadEncodedInt();

        if (length == 0)
        {
            return string.Empty;
        }

        var str = TextEncoding.GetString(new ReadOnlySpan<byte>(Take(length), length), _encoding);
        return intern ? str.Intern() : str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => BinaryPrimitives.ReadInt64LittleEndian(new ReadOnlySpan<byte>(Take(8), 8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => BinaryPrimitives.ReadUInt64LittleEndian(new ReadOnlySpan<byte>(Take(8), 8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(Take(4), 4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(Take(4), 4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => BinaryPrimitives.ReadInt16LittleEndian(new ReadOnlySpan<byte>(Take(2), 2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(Take(2), 2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => BinaryPrimitives.ReadDoubleLittleEndian(new ReadOnlySpan<byte>(Take(8), 8));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => BinaryPrimitives.ReadSingleLittleEndian(new ReadOnlySpan<byte>(Take(4), 4));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => *Take(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)*Take(1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => *Take(1) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Serial ReadSerial() => (Serial)ReadUInt();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadEncodedInt()
    {
        var b = *Take(1);
        return b < 0x80 ? b : ReadEncodedIntMultiByte(b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int ReadEncodedIntMultiByte(byte first)
    {
        var v = first & 0x7F;

        for (var shift = 7; shift < 35; shift += 7)
        {
            var b = *Take(1);
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
        var p = Take(12);
        return new Point3D(
            BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(p, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(p + 4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(p + 8, 4))
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Point2D ReadPoint2D()
    {
        var p = Take(8);
        return new Point2D(
            BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(p, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(p + 4, 4))
        );
    }

    public Rectangle2D ReadRect2D() => new(ReadPoint2D(), ReadPoint2D());

    public Rectangle3D ReadRect3D() => new(ReadPoint3D(), ReadPoint3D());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Map ReadMap() => Map.Maps[*Take(1)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Race ReadRace() => Race.Races[*Take(1)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadEnum<T>() where T : unmanaged, Enum
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
        var length = buffer.Length;
        new ReadOnlySpan<byte>(Take(length), length).CopyTo(buffer);
        return length;
    }

    public virtual long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End     => _size + offset,
            _                  => offset // Begin
        };

        if ((ulong)position > (ulong)_size)
        {
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                $"Seeking to {position} is outside the data (0 to {_size})."
            );
        }

        return _position = position;
    }
}
