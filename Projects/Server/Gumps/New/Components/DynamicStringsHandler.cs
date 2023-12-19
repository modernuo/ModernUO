/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicStringsHandler.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Collections;
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
        private static readonly byte[] buffer = GumpBuilder.StringsBuffer;
        private static readonly Dictionary<int, int> stringHashes = [];
        private static BitVector32 dynamicIndexes = new();
        private static int position;
        private static int dynamicCount;

        public int BytesWritten => position;
        public int Count => stringHashes.Count;

        public int Internalize(ReadOnlySpan<char> value)
        {
            int hash = string.GetHashCode(value);

            if (!stringHashes.TryGetValue(hash, out int index))
            {
                index = stringHashes.Count;

                stringHashes.Add(hash, index);

                if (value.StartsWith(GumpBuilder.DynamicStringPlaceholder))
                {
                    dynamicIndexes[index] = true;
                    dynamicCount++;
                    return index;
                }

                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(position), (ushort)value.Length);
                position += 2;

                if (BitConverter.IsLittleEndian)
                {
                    position += TextEncoding.Unicode.GetBytes(value, buffer.AsSpan(position));
                }
                else
                {
                    ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(value);
                    bytes.CopyTo(buffer.AsSpan(position));
                    position += bytes.Length;
                }
            }

            return index;
        }

        public void WriteCompressed(ref SpanWriter writer)
        {
            OutgoingGumpPackets.WritePacked(buffer.AsSpan(..position), ref writer);
        }

        public byte[] ToArray()
        {
            return buffer[..position];
        }

        public byte[] ToCompressedArray()
        {
            SpanWriter writer = new(Zlib.MaxPackSize(position));
            OutgoingGumpPackets.WritePacked(buffer.AsSpan(..position), ref writer);
            byte[] toRet = writer.Span.ToArray();
            writer.Dispose();

            return toRet;
        }

        public void Finalize(out DynamicStringsEntry entry)
        {
            entry = new([.. buffer[..position]], dynamicIndexes, stringHashes.Count, dynamicCount);
        }

        public void Dispose()
        {
            position = 0;
            stringHashes.Clear();
            dynamicIndexes = new();
            dynamicCount = 0;
        }
    }
}
