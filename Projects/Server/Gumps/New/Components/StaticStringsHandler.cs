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

using Server.Buffers;
using Server.Gumps.Interfaces;
using Server.Network;
using Server.Text;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Compression;

namespace Server.Gumps.Components
{
    public readonly struct StaticStringsHandler : IStringsHandler
    {
        private static readonly byte[] buffer = GumpBuilder.StringsBuffer;
        private static readonly Dictionary<int, int> stringHashes = [];
        private static int position;

        public int BytesWritten => position;
        public int Count => stringHashes.Count;

        public int Internalize(string? value)
        {
            value ??= "";

            int hash = value.GetHashCode();

            if (!stringHashes.TryGetValue(hash, out int index))
            {
                index = stringHashes.Count;

                stringHashes.Add(hash, index);

                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(position), (ushort)value.Length);
                position += 2;

                position += TextEncoding.Unicode.GetBytes(value.AsSpan(), buffer.AsSpan(position..));
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
            int worstLength = Zlib.MaxPackSize(position);
            byte[] compressedBuffer = STArrayPool<byte>.Shared.Rent(worstLength);

            return ToCompressedArray(compressedBuffer);
        }

        internal static byte[] ToCompressedArray(Span<byte> compressedBuffer)
        {
            SpanWriter writer = new(compressedBuffer);
            OutgoingGumpPackets.WritePacked(buffer.AsSpan(..position), ref writer);
            byte[] toRet = writer.Span.ToArray();
            writer.Dispose();

            return toRet;
        }

        public void Dispose()
        {
            position = 0;
            stringHashes.Clear();
        }
    }
}
