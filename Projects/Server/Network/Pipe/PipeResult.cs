/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PipeResult.cs                                                   *
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

namespace Server.Network
{
    public class PipeResult
    {
        public ArraySegment<byte>[] Buffer { get; }

        public int Length
        {
            get
            {
                var length = 0;
                for (int i = 0; i < Buffer.Length; i++)
                {
                    length += Buffer[i].Count;
                }

                return length;
            }
        }

        public void CopyFrom(ReadOnlySpan<byte> bytes)
        {
            var remaining = bytes.Length;
            var offset = 0;

            if (remaining == 0)
            {
                return;
            }

            for (int i = 0; i < Buffer.Length; i++)
            {
                var buffer = Buffer[i];
                var sz = Math.Min(remaining, buffer.Count);
                bytes.Slice(offset, sz).CopyTo(buffer.AsSpan());

                remaining -= sz;
                offset += sz;

                if (remaining == 0)
                {
                    return;
                }
            }

            throw new OutOfMemoryException();
        }

        public PipeResult(int segments) => Buffer = new ArraySegment<byte>[segments];
    }
}
