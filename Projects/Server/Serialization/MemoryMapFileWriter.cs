/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MemoryMapFileWriter.cs                                          *
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
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Server.Text;

namespace Server;

public unsafe class MemoryMapFileWriter : IGenericWriter, IDisposable
{
    private readonly Encoding _encoding;

    private readonly HashSet<Type> _types;
    private readonly FileStream _fileStream;
    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _accessor;
    private byte* _ptr;
    private long _position;
    private long _size;

    public MemoryMapFileWriter(FileStream fileStream, long initialSize, HashSet<Type> types = null)
    {
        _types = types;
        _fileStream = fileStream;
        _encoding = TextEncoding.UTF8;
        _size = Math.Max(initialSize, 1024);

        ResizeMemoryMappedFile(initialSize);
    }

    public long Position => _position;

    public FileStream FileStream => _fileStream;

    private void ResizeMemoryMappedFile(long newSize)
    {
        _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
        _accessor?.Dispose();
        _mmf?.Dispose();

        // Do the actual resizing
        _fileStream.SetLength(newSize);

        _mmf = MemoryMappedFile.CreateFromFile(_fileStream, null, newSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
        _accessor = _mmf.CreateViewAccessor();
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
    }

    private void EnsureCapacity(long bytesToWrite)
    {
        var shouldResize = false;
        while (_position + bytesToWrite > _size)
        {
            // Don't double forever, eventually we want to have a maximum, like 256MB at a time or something
            _size += Math.Min(_size, 1024 * 1024 * 256);
            shouldResize = true;
        }

        if (shouldResize)
        {
            ResizeMemoryMappedFile(_size);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte[] bytes) => Write(bytes.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte[] bytes, int offset, int count) => Write(bytes.AsSpan(offset, count));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> bytes)
    {
        var byteCount = bytes.Length;
        EnsureCapacity(byteCount);

        bytes.CopyTo(new Span<byte>(_ptr + _position, byteCount));
        _position += byteCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                {
                    if (offset > _size)
                    {
                        EnsureCapacity(offset);
                    }

                    _position = offset;
                    break;
                }
            case SeekOrigin.Current:
                {
                    EnsureCapacity(offset);
                    _position += offset;
                    break;
                }
            case SeekOrigin.End:
                {
                    if (_position + offset > _size)
                    {
                        EnsureCapacity(offset);
                    }

                    _position = _size + offset;

                    if (_position < 0)
                    {
                        Dispose();
                        throw new InvalidOperationException("Seek before start of file");
                    }
                    break;
                }
        }

        return _position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(string value)
    {
        if (value == null)
        {
            Write(false);
        }
        else
        {
            Write(true);
            WriteStringRaw(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(long value)
    {
        EnsureCapacity(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(new Span<byte>(_ptr + _position, sizeof(long)), value);
        _position += sizeof(long);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ulong value)
    {
        EnsureCapacity(sizeof(ulong));
        BinaryPrimitives.WriteUInt64LittleEndian(new Span<byte>(_ptr + _position, sizeof(ulong)), value);
        _position += sizeof(ulong);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
        EnsureCapacity(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(new Span<byte>(_ptr + _position, sizeof(int)), value);
        _position += sizeof(int);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
        EnsureCapacity(sizeof(uint));
        BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(_ptr + _position, sizeof(uint)), value);
        _position += sizeof(uint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(short value)
    {
        EnsureCapacity(sizeof(short));
        BinaryPrimitives.WriteInt16LittleEndian(new Span<byte>(_ptr + _position, sizeof(short)), value);
        _position += sizeof(short);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ushort value)
    {
        EnsureCapacity(sizeof(ushort));
        BinaryPrimitives.WriteUInt16LittleEndian(new Span<byte>(_ptr + _position, sizeof(ushort)), value);
        _position += sizeof(ushort);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(double value)
    {
        EnsureCapacity(sizeof(double));
        BinaryPrimitives.WriteDoubleLittleEndian(new Span<byte>(_ptr + _position, sizeof(double)), value);
        _position += sizeof(double);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(float value)
    {
        EnsureCapacity(sizeof(float));
        BinaryPrimitives.WriteSingleLittleEndian(new Span<byte>(_ptr + _position, sizeof(float)), value);
        _position += sizeof(float);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value)
    {
        EnsureCapacity(1);
        *(_ptr + _position++) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(sbyte value) => Write((byte)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(bool value) => Write(*(byte*)&value);

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
            _types.Add(type);
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
    public void WriteStringRaw(ReadOnlySpan<char> value)
    {
        var length = _encoding.GetByteCount(value);

        EnsureCapacity(length + 5);

        // WriteEncodedInt
        var v = (uint)length;

        while (v >= 0x80)
        {
            *(_ptr + _position++) = (byte)(v | 0x80);
            v >>= 7;
        }
        *(_ptr + _position++) = (byte)v;

        _encoding.GetBytes(value, new Span<byte>(_ptr + _position, length));
        _position += length;
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor.Dispose();
            _mmf.Dispose();

            // Truncate the file
            _fileStream.SetLength(_position);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~MemoryMapFileWriter()
    {
        Dispose(false);
    }
}
