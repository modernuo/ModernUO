/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicStringsEntry.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Network;
using Server.Text;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

namespace Server.Gumps.Components
{
    public readonly struct DynamicStringsEntry
    {
        private static readonly byte[] buffer = GumpBuilder.StringsBuffer;

        private readonly int dynamicCounts;
        private readonly BitVector32 dynamicEntries;
        public readonly byte[] Data;
        public readonly int Count;

        public DynamicStringsEntry(byte[] data, BitVector32 dynamicEntries, int stringsCount, int dynamicCounts)
        {
            Data = data;
            this.dynamicEntries = dynamicEntries;
            Count = stringsCount;
            this.dynamicCounts = dynamicCounts;
        }

        public StringsEntry Build(params string[] strings)
        {
            int size = PrepareBuffer(strings);
            return new(buffer[..size], Count, false, size);
        }

        public StringsEntry BuildCompressed(params string[] strings)
        {
            int size = PrepareBuffer(strings);

            SpanWriter writer = new(buffer.AsSpan(size));
            OutgoingGumpPackets.WritePacked(buffer.AsSpan(..size), ref writer);

            return new(writer.Span.ToArray(), Count, true, size);
        }

        private int PrepareBuffer(string[] strings)
        {
            if (strings.Length != dynamicCounts)
            {
                throw new ArgumentException($"String entries count mismatch. Expected: {dynamicCounts}, given: {strings.Length}");
            }

            int bufferPosition = 0;
            int dataPosition = 0;
            int stringIndex = 0;

            for (int i = 0; i < Count; i++)
            {
                if (dynamicEntries[i])
                {
                    string s = strings[stringIndex++];

                    BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(bufferPosition), (ushort)s.Length);
                    bufferPosition += 2;

                    if (BitConverter.IsLittleEndian)
                    {
                        bufferPosition += TextEncoding.Unicode.GetBytes(s.AsSpan(), buffer.AsSpan(bufferPosition));
                    }
                    else
                    {
                        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(s.AsSpan());
                        bytes.CopyTo(buffer.AsSpan(bufferPosition));
                        bufferPosition += bytes.Length;
                    }
                }
                else
                {
                    ushort stringLength = BinaryPrimitives.ReadUInt16BigEndian(Data.AsSpan(dataPosition, 2));
                    dataPosition += 2;

                    BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(bufferPosition), stringLength);
                    bufferPosition += 2;

                    int bytesLength = stringLength * 2; // 2 bytes per char
                    Data.AsSpan(dataPosition, bytesLength).CopyTo(buffer.AsSpan(bufferPosition));
                    bufferPosition += bytesLength;
                }
            }

            return bufferPosition;
        }
    }
}
