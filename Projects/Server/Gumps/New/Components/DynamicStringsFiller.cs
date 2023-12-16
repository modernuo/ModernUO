/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicStringsFiller.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Text;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Runtime.InteropServices;

namespace Server.Gumps.Components;

public ref struct DynamicStringsFiller
{
    private static readonly byte[] _buffer = GumpBuilderExtensions.StringsBuffer;

    private readonly BitArray _dynamicEntries;
    private readonly byte[] _data;
    private int _bufferPosition;
    private int _dataPosition;
    private int _index;

    public readonly int UncompressedLength => _bufferPosition;
    public readonly int Count => _dynamicEntries.Count;
    public readonly ReadOnlySpan<byte> Data => _buffer.AsSpan(0, _bufferPosition);

    public DynamicStringsFiller(BitArray dynamicEntries, byte[] data)
    {
        _dynamicEntries = dynamicEntries;
        _data = data;
        _bufferPosition = 0;
        _dataPosition = 0;
        _index = 0;

        WriteNextStaticStringsChunk();
    }

    public void Add(ReadOnlySpan<char> value)
    {
        if (_index >= _dynamicEntries.Count)
        {
            throw new Exception();
        }

        BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_bufferPosition), (ushort)value.Length);
        _bufferPosition += 2;

        if (BitConverter.IsLittleEndian)
        {
            _bufferPosition += TextEncoding.Unicode.GetBytes(value, _buffer.AsSpan(_bufferPosition));
        }
        else
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(value);
            bytes.CopyTo(_buffer.AsSpan(_bufferPosition));
            _bufferPosition += bytes.Length;
        }

        _index++;
        WriteNextStaticStringsChunk();
    }

    private void WriteNextStaticStringsChunk()
    {
        for (; _index < _dynamicEntries.Count; _index++)
        {
            if (_dynamicEntries[_index])
            {
                break;
            }

            ushort stringLength = BinaryPrimitives.ReadUInt16BigEndian(_data.AsSpan(_dataPosition, 2));
            _dataPosition += 2;

            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_bufferPosition), stringLength);
            _bufferPosition += 2;

            int bytesLength = stringLength * 2; // 2 bytes per char
            _data.AsSpan(_dataPosition, bytesLength).CopyTo(_buffer.AsSpan(_bufferPosition));
            _bufferPosition += bytesLength;
        }
    }

    internal readonly bool Finalize()
    {
        return _index >= _dynamicEntries.Count;
    }
}