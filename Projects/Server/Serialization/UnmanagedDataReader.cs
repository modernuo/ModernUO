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

/// <summary>
/// Read bits of data raw from a serialized file using Little-endian.
/// </summary>
public unsafe class UnmanagedDataReader : IGenericReader
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(UnmanagedDataReader));

    private readonly byte* _ptr;
    private readonly long _size;

    private readonly Dictionary<ulong, string> _typesDb;
    private readonly Encoding _encoding;

    /// <summary>
    /// How many bits deep into the file is the reader at currently.
    /// </summary>
    public long Position { get; private set; }

    /// <summary>
    /// Read bits of data raw from a serialized file using Little-endian.
    /// </summary>
    /// <param name="ptr">The starting address for reading bits.</param>
    /// <param name="size">The total size of memory to be read.</param>
    /// <param name="typesDb">The custom type dictionary. Will throw an error if left null ReadType is called.</param>
    /// <param name="encoding"><see cref="TextEncoding.UTF8"/> by default.</param>
    public UnmanagedDataReader(byte* ptr, long size, Dictionary<ulong, string> typesDb = null, Encoding encoding = null)
    {
        _encoding = encoding ?? TextEncoding.UTF8;
        _typesDb = typesDb;
        _ptr = ptr;
        _size = size;
    }

    /// <summary>
    /// Reads the next bit as a bool. If that bit is true, return an <see cref="ReadStringRaw"/> else null.
    /// </summary>
    /// <param name="intern"></param>
    /// <returns>Next string value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(bool intern = false) => ReadBool() ? ReadStringRaw(intern) : null;

    /// <summary>
    /// Returns the next set of bits that make up a string.
    /// </summary>
    /// <param name="intern"></param>
    /// <returns>Next string value.</returns>
    public string ReadStringRaw(bool intern = false)
    {
        // ReadEncodedInt
        int length = 0, shift = 0;
        byte b;

        do
        {
            b = *(_ptr + Position++);
            length |= (b & 0x7F) << shift;
            shift += 7;
        }
        while (b >= 0x80);

        if (length <= 0)
        {
            return "".Intern();
        }

        var str = TextEncoding.GetString(new ReadOnlySpan<byte>(_ptr + Position, length), _encoding);
        Position += length;
        return intern ? str.Intern() : str;
    }

    /// <summary>
    /// Read the next 64 bits to make up a long (int64).
    /// </summary>
    /// <returns>Next long value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong()
    {
        var v = BinaryPrimitives.ReadInt64LittleEndian(new ReadOnlySpan<byte>(_ptr + Position, sizeof(long)));
        Position += sizeof(long);
        return v;
    }

    /// <summary>
    /// Read the next 64 bits to make up an unsigned long (uint64).
    /// </summary>
    /// <returns>Next ulong value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong()
    {
        var v = BinaryPrimitives.ReadUInt64LittleEndian(new ReadOnlySpan<byte>(_ptr + Position, sizeof(ulong)));
        Position += sizeof(ulong);
        return v;
    }

    /// <summary>
    /// Read the next 32 bits to make up an int (int32).
    /// </summary>
    /// <returns>Next int value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt()
    {
        var v = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(_ptr + Position, sizeof(int)));
        Position += sizeof(int);
        return v;
    }

    /// <summary>
    /// Read the next 32 bits to make up an unsigned int (uint32).
    /// </summary>
    /// <returns>Next uint value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt()
    {
        var v = BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(_ptr + Position, sizeof(uint)));
        Position += sizeof(uint);
        return v;
    }

    /// <summary>
    /// Read the next 16 bits to make up a short (int16).
    /// </summary>
    /// <returns>Next short value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort()
    {
        var v = BinaryPrimitives.ReadInt16LittleEndian(new ReadOnlySpan<byte>(_ptr + Position, sizeof(short)));
        Position += sizeof(short);
        return v;
    }

    /// <summary>
    /// Read the next 16 bits to make up an unsigned short (int16).
    /// </summary>
    /// <returns>Next ushort value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
        var v = BinaryPrimitives.ReadUInt16LittleEndian(new ReadOnlySpan<byte>(_ptr + Position, sizeof(ushort)));
        Position += sizeof(ushort);
        return v;
    }

    /// <summary>
    /// Read the next 8 bits to make up a float point double.
    /// </summary>
    /// <returns>Next double value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble()
    {
        var v = BinaryPrimitives.ReadDoubleLittleEndian(new ReadOnlySpan<byte>(_ptr + Position, sizeof(double)));
        Position += sizeof(double);
        return v;
    }

    /// <summary>
    /// Read the next 4 bits to make up a float point.
    /// </summary>
    /// <returns>Next float value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat()
    {
        var v = BinaryPrimitives.ReadSingleLittleEndian(new ReadOnlySpan<byte>(_ptr + Position, sizeof(float)));
        Position += sizeof(float);
        return v;
    }

    /// <summary>
    /// Read the next 8 bits to make up a byte.
    /// </summary>
    /// <returns>Next byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => *(_ptr + Position++);
    /// <summary>
    /// Read the next 8 bits to make up a signed byte.
    /// </summary>
    /// <returns>Next sbyte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)ReadByte();
    /// <summary>
    /// Read the next Byte and return true if it's value returns zero.
    /// </summary>
    /// <returns>Next bool value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => ReadByte() != 0;

    /// <summary>
    /// Read the next 32 bytes to make up a <see cref="Serial"/>.
    /// </summary>
    /// <returns>Next uint value cast as a <see cref="Serial"/> struct.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Serial ReadSerial() => (Serial)ReadUInt();

    /// <summary>
    /// Reads the next Byte which helps determine how to read the following Type.
    /// <br>If the byte returns 1 => <see cref="ReadStringRaw"/> and translate into a Type via the <see cref="AssemblyHandler"/></br>
    /// <br>If the byte returns 2 => <see cref="ReadTypeByHash"/></br>
    /// <br>else return null</br>
    /// </summary>
    /// <returns>Next Type value</returns>
    public Type ReadType() =>
        ReadByte() switch
        {
            1 => AssemblyHandler.FindTypeByFullName(ReadStringRaw()), // Backward compatibility
            2 => ReadTypeByHash(),
            _ => null,
        };

    /// <summary>
    /// Reads the next <see cref="ulong"/> to create a hash and convert that into a Type using the <see cref="AssemblyHandler"/>.
    /// <para>Will log an <see cref="Exception"/> if typesDb is null or typesDb doesn't contain the hash and return null</para>
    /// </summary>
    /// <returns>Next Type value</returns>
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

    /// <summary>
    /// Reads the next set of bytes to fill the buffer.
    /// </summary>
    /// <param name="buffer">A reference span that will be filled with the next set of bytes.</param>
    /// <returns>The length of the buffer.</returns>
    /// <exception cref="OutOfMemoryException">Thrown if the buffer is larger than the remaining data to read in the file.</exception>
    public int Read(Span<byte> buffer)
    {
        var length = buffer.Length;
        if (length > _size - Position)
        {
            throw new OutOfMemoryException();
        }

        new ReadOnlySpan<byte>(_ptr + Position, length).CopyTo(buffer);
        Position += length;
        return length;
    }

    /// <summary>
    /// Sets the current position of the stream to a specified value.
    /// </summary>
    /// <param name="offset">The new position, relative to the <paramref name="origin"/> parameter.</param>
    /// <param name="origin">The reference point for the <paramref name="offset"/> parameter. It can be one of the values of <see cref="SeekOrigin"/>.</param>
    /// <returns>The new position in the stream, in bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="offset"/> or the resulting position is out of the valid range.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the stream does not support seeking.</exception>
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
            origin != SeekOrigin.Current || Position + offset >= 0 && Position + offset < _size,
            "Attempting to seek to an invalid position using SeekOrigin.Current"
        );

        Position = Math.Max(0L, origin switch
        {
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End     => _size + offset,
            _                  => offset // Begin
        });
        return Position;
    }
}
