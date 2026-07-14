/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationChunkSource.cs                                     *
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
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server;

/// <summary>
/// Single-producer/multi-consumer handoff between the game loop and the serialization
/// thread workers during a world save. The producer batches entities into pooled chunks
/// so the per-entity cost is a plain array store instead of a synchronized enqueue, and
/// workers pull whole chunks so they naturally load-balance: a worker busy with a thick
/// entity simply takes fewer chunks.
/// Entities whose previous serialized size exceeds <see cref="HeavyEntityThreshold"/> are
/// published as dedicated single-entity chunks so multi-megabyte payloads spread across
/// workers instead of riding inside one chunk.
/// </summary>
public sealed class SerializationChunkSource
{
    // 4096 refs (32KB per chunk) keeps producer sync cost at one enqueue per 4096 entities
    // while the drain tail stays sub-millisecond.
    private const int ChunkCapacity = 4096;

    /// <summary>
    /// Entities whose previous <see cref="IGenericSerializable.SerializedLength"/> exceeds this
    /// should be pushed with <see cref="PushSingle"/>. Callers do the check where the entity's
    /// concrete type is known, so the size read is not an interface dispatch per entity.
    /// </summary>
    public const int HeavyEntityThreshold = 1024 * 1024; // 1MB

    internal readonly struct Chunk
    {
        public readonly IGenericSerializable Single;
        public readonly IGenericSerializable[] Buffer;
        public readonly int Count;

        public Chunk(IGenericSerializable single)
        {
            Single = single;
            Buffer = null;
            Count = 1;
        }

        public Chunk(IGenericSerializable[] buffer, int count)
        {
            Single = null;
            Buffer = buffer;
            Count = count;
        }
    }

    private readonly ConcurrentQueue<Chunk> _chunks = new();
    private readonly ConcurrentQueue<IGenericSerializable[]> _pool = new();

    // Producer state - written only by the game loop thread.
    private IGenericSerializable[] _current;
    private int _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(IGenericSerializable entity)
    {
        var current = _current ??= Rent();

        // Ref store skips the bounds and array-covariance checks. Safe by construction:
        // _count is producer-thread-only and always < ChunkCapacity here (reset on publish),
        // and the array's element type is exactly IGenericSerializable.
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(current), _count) = entity;

        if (++_count == ChunkCapacity)
        {
            _chunks.Enqueue(new Chunk(current, ChunkCapacity));
            _current = null;
            _count = 0;
        }
    }

    /// <summary>
    /// Publishes the entity as a dedicated chunk regardless of its estimated size.
    /// Used for persistence self-payloads, which can be large on the first save
    /// before a <see cref="IGenericSerializable.SerializedLength"/> estimate exists.
    /// </summary>
    public void PushSingle(IGenericSerializable entity) => _chunks.Enqueue(new Chunk(entity));

    /// <summary>
    /// Publishes the partial chunk, if any. Must be called on the producer thread before
    /// the workers are told to finish draining, or the tail of the stream is not serialized.
    /// </summary>
    public void Flush()
    {
        if (_count > 0)
        {
            _chunks.Enqueue(new Chunk(_current, _count));
            _current = null;
            _count = 0;
        }
    }

    internal bool TryTake(out Chunk chunk) => _chunks.TryDequeue(out chunk);

    internal void Return(IGenericSerializable[] buffer, int count)
    {
        // Clear so pooled chunks don't keep entities reachable between saves.
        Array.Clear(buffer, 0, count);
        _pool.Enqueue(buffer);
    }

    private IGenericSerializable[] Rent() =>
        _pool.TryDequeue(out var buffer) ? buffer : new IGenericSerializable[ChunkCapacity];
}
