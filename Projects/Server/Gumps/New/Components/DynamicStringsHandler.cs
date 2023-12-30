/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicStringsHandler.cs                                        *
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
using System.Collections.Specialized;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Server.Gumps.Components
{
    public readonly struct DynamicStringsHandler : IStringsHandler
    {
        private static readonly byte[] _buffer = GumpBuilder.StringsBuffer;
        private static readonly Dictionary<int, int> _stringHashes = [];
        private static BitVector32 _dynamicIndexes = new();
        private static int _position;
        private static int _dynamicCount;

        public int BytesWritten => _position;
        public int Count => _stringHashes.Count;

        public int Internalize(ReadOnlySpan<char> value)
        {
            int hash = string.GetHashCode(value);

            if (!_stringHashes.TryGetValue(hash, out int index))
            {
                index = _stringHashes.Count;

                _stringHashes.Add(hash, index);

                if (value.StartsWith(GumpBuilder.DynamicStringPlaceholder))
                {
                    _dynamicIndexes[index] = true;
                    _dynamicCount++;
                    return index;
                }

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

        public void WriteCompressed(ref SpanWriter writer)
        {
            OutgoingGumpPackets.WritePacked(_buffer.AsSpan(.._position), ref writer);
        }

        public byte[] ToArray()
        {
            return _buffer[.._position];
        }

        public byte[] ToCompressedArray()
        {
            SpanWriter writer = new(Zlib.MaxPackSize(_position));
            OutgoingGumpPackets.WritePacked(_buffer.AsSpan(.._position), ref writer);
            byte[] toRet = writer.Span.ToArray();
            writer.Dispose();

            return toRet;
        }

        public void Finalize(out DynamicStringsEntry entry)
        {
            entry = new([.. _buffer[.._position]], _dynamicIndexes, _stringHashes.Count, _dynamicCount);
        }

        public void Dispose()
        {
            _position = 0;
            _stringHashes.Clear();
            _dynamicIndexes = new();
            _dynamicCount = 0;
        }
    }
}
