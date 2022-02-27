/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: PacketContainerBuilder.cs                                       *
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
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Server.Network;

public ref struct PacketContainerBuilder
{
    public const int MinPacketLength = 5;

    private bool _finished;
    private int _count;

    private byte[] _arrayToReturnToPool;
    private Span<byte> _bytes;

    public PacketContainerBuilder(Span<byte> initialBuffer)
    {
        _arrayToReturnToPool = null;
        _finished = false;
        _count = 0;

        _bytes = initialBuffer;
        _bytes[0] = 0xF7;         // Packet ID
        Length = MinPacketLength; // Length + Count
    }

    public int Length { get; set; }

    public int Capacity => _bytes.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ReadOnlySpan<byte> Finalize()
    {
        if (!_finished)
        {
            BinaryPrimitives.WriteUInt16BigEndian(_bytes[1..3], (ushort)Length);
            BinaryPrimitives.WriteUInt16BigEndian(_bytes[3..5], (ushort)_count);
        }

        return _bytes[..Length];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Span<byte> GetSpan(int bytesNeeded)
    {
        if (_finished)
        {
            throw new InvalidOperationException("Attempted to use PacketContainerBuilder after finalize");
        }

        if (Length > _bytes.Length - bytesNeeded)
        {
            Grow(bytesNeeded);
        }

        return _bytes[Length..];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Advance(int bytesWritten)
    {
        if (_finished)
        {
            throw new InvalidOperationException("Attempted to use PacketContainerBuilder after finalize");
        }

        _count++;
        Length += bytesWritten;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos)
    {
        var newLength = Math.Max(Length + additionalCapacityBeyondPos, _bytes.Length * 2);
        byte[] poolArray = ArrayPool<byte>.Shared.Rent(newLength);

        _bytes[..Length].CopyTo(poolArray);

        byte[] toReturn = _arrayToReturnToPool;
        _bytes = _arrayToReturnToPool = poolArray;
        if (toReturn != null)
        {
            ArrayPool<byte>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        byte[] toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn != null)
        {
            ArrayPool<byte>.Shared.Return(toReturn);
        }
    }
}
