/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BinaryFileReader.cs                                             *
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
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server;

/// <summary>
/// Read bits of data from a serialized file in a managed environment.
/// </summary>
public sealed unsafe class BinaryFileReader : IDisposable, IGenericReader
{
    private readonly bool _usePrefixes;
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewStream _accessor;
    private readonly UnmanagedDataReader _reader;

    public BinaryFileReader(string path, bool usePrefixes = true, Encoding encoding = null)
    {
        _usePrefixes = usePrefixes;
        var fi = new FileInfo(path);

        if (fi.Length > 0)
        {
            _mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
            _accessor = _mmf.CreateViewStream();
            byte* ptr = null;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            _reader = new UnmanagedDataReader(ptr, _accessor.Length, encoding: encoding);
        }
        else
        {
            _reader = new UnmanagedDataReader(null, 0, encoding: encoding);
        }
    }

    public long Position => _reader.Position;

    public void Dispose()
    {
        _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
        _accessor?.Dispose();
        _mmf?.Dispose();
    }

    /// <summary>
    /// If usePrefixes is true, return a ReadString(intern) else ReadStringRaw(intern).
    /// </summary>
    /// <returns>A string value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(bool intern = false) => _usePrefixes ? _reader.ReadString(intern) : _reader.ReadStringRaw(intern);
    /// <summary>
    /// Returns the next set of bits that make up a string.
    /// </summary>
    /// <returns>Next string value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadStringRaw(bool intern = false) => _reader.ReadStringRaw(intern);
    /// <summary>
    /// Read the next 64 bits to make up a long (int64).
    /// </summary>
    /// <returns>Next long value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => _reader.ReadLong();
    /// <summary>
    /// Read the next 64 bits to make up an unsigned long (uint64).
    /// </summary>
    /// <returns>Next ulong value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => _reader.ReadULong();
    /// <summary>
    /// Read the next 32 bits to make up an int (int32).
    /// </summary>
    /// <returns>Next int value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => _reader.ReadInt();
    /// <summary>
    /// Read the next 32 bits to make up an unsigned int (uint32).
    /// </summary>
    /// <returns>Next uint value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => _reader.ReadUInt();
    /// <summary>
    /// Read the next 16 bits to make up a short (int16).
    /// </summary>
    /// <returns>Next short value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => _reader.ReadShort();
    /// <summary>
    /// Read the next 16 bits to make up an unsigned short (int16).
    /// </summary>
    /// <returns>Next ushort value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => _reader.ReadUShort();
    /// <summary>
    /// Read the next 8 bits to make up a float point double.
    /// </summary>
    /// <returns>Next double value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => _reader.ReadDouble();
    /// <summary>
    /// Read the next 4 bits to make up a float point.
    /// </summary>
    /// <returns>Next float value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => _reader.ReadFloat();
    /// <summary>
    /// Read the next 8 bits to make up a byte.
    /// </summary>
    /// <returns>Next byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => _reader.ReadByte();
    /// <summary>
    /// Read the next 8 bits to make up a signed byte.
    /// </summary>
    /// <returns>Next sbyte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => _reader.ReadSByte();
    /// <summary>
    /// Read the next 1 bit to make up a boolean.
    /// </summary>
    /// <returns>Next bool value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => _reader.ReadBool();
    /// <summary>
    /// Read the next 32 bytes to make up a Serial.
    /// </summary>
    /// <returns>Next uint value cast as a Serial struct.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Serial ReadSerial() => _reader.ReadSerial();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Type ReadType() => _reader.ReadType();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(Span<byte> buffer) => _reader.Read(buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Seek(long offset, SeekOrigin origin) => _reader.Seek(offset, origin);
}
