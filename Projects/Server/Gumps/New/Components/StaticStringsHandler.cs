/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: StaticStringsHandler.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps.Interfaces;
using Server.Network;
using Server.Text;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Server.Gumps.Components;

public readonly struct StaticStringsHandler : IStringsHandler
{
    private static readonly byte[] _buffer = GumpBuilderExtensions.StringsBuffer;
    private static readonly Dictionary<int, int> _stringHashes = [];
    private static int _position;

    public int BytesWritten => _position;
    public int Count => _stringHashes.Count;
    public ReadOnlySpan<byte> Span => _buffer.AsSpan(0, _position);

    public int Internalize(ReadOnlySpan<char> value)
    {
        int hash = string.GetHashCode(value);

        if (!_stringHashes.TryGetValue(hash, out int index))
        {
            index = _stringHashes.Count;

            _stringHashes.Add(hash, index);

            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position), (ushort)value.Length);
            _position += 2;

            if (BitConverter.IsLittleEndian)
            {
                _position += TextEncoding.Unicode.GetBytes(value, _buffer.AsSpan(_position));
            }
            else
            {
                ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(value);
                bytes.CopyTo(_buffer.AsSpan(_position));
                _position += bytes.Length;
            }
        }

        return index;
    }

    internal StaticStringsEntry Finalize(Span<byte> compressionBuffer)
    {
        SpanWriter writer = new(compressionBuffer);
        OutgoingGumpPackets.WritePacked(_buffer.AsSpan(.._position), ref writer);
        byte[] toRet = writer.Span.ToArray();
        writer.Dispose();

        return new(toRet, _position, _stringHashes.Count);
    }

    public void Dispose()
    {
        _position = 0;
        _stringHashes.Clear();
    }
}
