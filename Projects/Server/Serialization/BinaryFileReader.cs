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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(bool intern = false) => _usePrefixes ? _reader.ReadString(intern) : _reader.ReadStringRaw(intern);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadStringRaw(bool intern = false) => _reader.ReadStringRaw(intern);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => _reader.ReadLong();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => _reader.ReadULong();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => _reader.ReadInt();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => _reader.ReadUInt();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => _reader.ReadShort();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => _reader.ReadUShort();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => _reader.ReadDouble();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => _reader.ReadFloat();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => _reader.ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => _reader.ReadSByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => _reader.ReadBool();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Serial ReadSerial() => _reader.ReadSerial();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Type ReadType() => _reader.ReadType();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(Span<byte> buffer) => _reader.Read(buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Seek(long offset, SeekOrigin origin) => _reader.Seek(offset, origin);
}
