/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using System.Runtime.CompilerServices;
using System.Text;
using Server.Buffers;
using Server.Collections;
using Server.Text;

namespace Server;

public class BinaryFileReader : IGenericReader, IDisposable
{
    private BinaryReader _reader;
    private Encoding _encoding;

    public BinaryFileReader(BinaryReader br, Encoding encoding = null)
    {
        _reader = br;
        _encoding = encoding ?? TextEncoding.UTF8;
    }

    public BinaryFileReader(Stream stream, Encoding encoding = null) : this(new BinaryReader(stream), encoding)
    {
    }

    public long Position => _reader.BaseStream.Position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Close() => _reader.Close();

    public DateTime LastSerialized { get; init; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        byte[] buffer = STArrayPool<byte>.Shared.Rent(length);
        var str = TextEncoding.GetString(buffer.AsSpan(0, length), _encoding);
        STArrayPool<byte>.Shared.Return(buffer);
        return intern ? Utility.Intern(str) : str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLong() => _reader.ReadInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong() => _reader.ReadUInt64();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt() => _reader.ReadInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt() => _reader.ReadUInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort() => _reader.ReadInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort() => _reader.ReadUInt16();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble() => _reader.ReadDouble();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat() => _reader.ReadSingle();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => _reader.ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => _reader.ReadSByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => _reader.ReadBoolean();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Serial ReadSerial() => (Serial)_reader.ReadUInt32();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(Span<byte> buffer) => _reader.Read(buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitArray ReadBitArray()
    {
        var length = ((IGenericReader)this).ReadEncodedInt();

        // BinaryReader doesn't expose a Span slice of the buffer, so we use a custom ctor
        return new BitArray(_reader, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long Seek(long offset, SeekOrigin origin) => _reader.BaseStream.Seek(offset, origin);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => Close();
}
