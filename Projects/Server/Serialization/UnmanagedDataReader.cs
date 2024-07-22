/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Logging;
using Server.Text;

namespace Server;

public unsafe class UnmanagedDataReader : IGenericReader
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(UnmanagedDataReader));

    private readonly byte* _ptr;
    private long _position;
    private readonly long _size;

    private readonly Dictionary<ulong, string> _typesDb;
    private readonly Encoding _encoding;

    public long Position => _position;

    public UnmanagedDataReader(byte* ptr, long size, Dictionary<ulong, string> typesDb = null, Encoding encoding = null)
    {
        _encoding = encoding ?? TextEncoding.UTF8;
        _typesDb = typesDb;
        _ptr = ptr;
        _size = size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(bool intern = false) => ReadBool() ? ReadStringRaw(intern) : null;

    public string ReadStringRaw(bool intern = false)
    {
        // ReadEncodedInt
        int length = 0, shift = 0;
        byte b;

        do
        {
            b = *(_ptr + _position++);
            length |= (b & 0x7F) << shift;
            shift += 7;
        }
        while (b >= 0x80);

        if (length <= 0)
        {
            return "".Intern();
        }

        var str = TextEncoding.GetString(new ReadOnlySpan<byte>(_ptr + _position, length), _encoding);
        _position += length;
        return intern ? str.Intern() : str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong()
    {
        var v = BinaryPrimitives.ReadInt64LittleEndian(new ReadOnlySpan<byte>(_ptr + _position, sizeof(long)));
        _position += sizeof(long);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong()
    {
        var v = BinaryPrimitives.ReadUInt64LittleEndian(new ReadOnlySpan<byte>(_ptr + _position, sizeof(ulong)));
        _position += sizeof(ulong);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt()
    {
        var v = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(_ptr + _position, sizeof(int)));
        _position += sizeof(int);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt()
    {
        var v = BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(_ptr + _position, sizeof(uint)));
        _position += sizeof(uint);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort()
    {
        var v = BinaryPrimitives.ReadInt16LittleEndian(new ReadOnlySpan<byte>(_ptr + _position, sizeof(short)));
        _position += sizeof(short);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
        var v = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(_ptr + _position, sizeof(ushort)));
        _position += sizeof(ushort);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble()
    {
        var v = BinaryPrimitives.ReadDoubleLittleEndian(new ReadOnlySpan<byte>(_ptr + _position, sizeof(double)));
        _position += sizeof(double);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat()
    {
        var v = BinaryPrimitives.ReadSingleLittleEndian(new ReadOnlySpan<byte>(_ptr + _position, sizeof(float)));
        _position += sizeof(float);
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => *(_ptr + _position++);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => ReadByte() != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Serial ReadSerial() => (Serial)ReadUInt();

    public Type ReadType() =>
        ReadByte() switch
        {
            0 => null,
            1 => AssemblyHandler.FindTypeByFullName(ReadStringRaw()), // Backward compatibility
            2 => ReadTypeByHash()
        };

    public Type ReadTypeByHash()
    {
        var hash = ReadULong();
        var t = AssemblyHandler.FindTypeByHash(hash);

        if (t != null)
        {
            return t;
        }

        if (_typesDb == null)
        {
            logger.Error(
                new Exception($"The file SerializedTypes.db was not loaded. Type hash '{hash}' could not be found."),
                "Invalid {Hash} at position {Position}",
                hash,
                Position
            );

            return null;
        }

        if (!_typesDb.TryGetValue(hash, out var typeName))
        {
            logger.Error(
                new Exception($"Type hash '{hash}' is not present in the serialized types database."),
                "Invalid type hash {Hash} at position {Position}",
                hash,
                Position
            );

            return null;
        }

        t = AssemblyHandler.FindTypeByFullName(typeName, false);

        if (t == null)
        {
            logger.Error(
                new Exception($"Type '{typeName}' was not found."),
                "Type {Type} was not found.",
                typeName
            );
        }

        return t;
    }

    public int Read(Span<byte> buffer)
    {
        var length = buffer.Length;
        if (length > _size - _position)
        {
            throw new OutOfMemoryException();
        }

        new ReadOnlySpan<byte>(_ptr + _position, length).CopyTo(buffer);
        _position += length;
        return length;
    }

    public virtual long Seek(long offset, SeekOrigin origin)
    {
        Debug.Assert(
            origin != SeekOrigin.End || offset <= 0 && offset > _size,
            "Attempting to seek to an invalid position using SeekOrigin.End"
        );
        Debug.Assert(
            origin != SeekOrigin.Begin || offset >= 0 && offset < _size,
            "Attempting to seek to an invalid position using SeekOrigin.Begin"
        );
        Debug.Assert(
            origin != SeekOrigin.Current || _position + offset >= 0 && _position + offset < _size,
            "Attempting to seek to an invalid position using SeekOrigin.Current"
        );

        var position = Math.Max(0L, origin switch
        {
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End     => _size + offset,
            _                  => offset // Begin
        });

        _position = position;
        return _position;
    }
}
