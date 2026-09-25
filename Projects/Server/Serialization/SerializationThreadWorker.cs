/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializationThreadWorker.cs                                    *
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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Server;

/// <summary>
/// One contiguous run of records a worker serialized into its heap from a single chunk.
/// Together with the worker's status log this replaces per-record placement state:
/// heap positions are implicit (a worker's writes are contiguous), and identity comes from
/// re-walking the same slots for range segments, or from the entities log for buffer
/// segments. The snapshot writer routes segments to files by <see cref="Owner"/>.
/// </summary>
internal readonly struct SerializedSegment
{
    public readonly object Owner;      // ISlotRangeSource for range segments, Persistence for buffer segments
    public readonly int SlotOffset;    // -1 when the segment came from a buffer chunk
    public readonly int SlotCount;
    public readonly long HeapStart;
    public readonly int StatusStart;
    public readonly int RecordCount;  // occupied slots logged, whatever their status
    public readonly int EntitiesStart; // buffer segments only

    public SerializedSegment(
        object owner, int slotOffset, int slotCount, long heapStart, int statusStart, int recordCount,
        int entitiesStart
    )
    {
        Owner = owner;
        SlotOffset = slotOffset;
        SlotCount = slotCount;
        HeapStart = heapStart;
        StatusStart = statusStart;
        RecordCount = recordCount;
        EntitiesStart = entitiesStart;
    }
}

public class SerializationThreadWorker
{
    private const int MinHeapSize = 1024 * 1024; // 1MB
    private readonly int _index;
    private readonly Thread _thread;
    private readonly AutoResetEvent _startEvent; // Main thread tells the thread to start working
    private readonly AutoResetEvent _stopEvent; // Main thread waits for the worker finish draining
    private readonly SerializationChunkSource _chunkSource;
    private readonly int _heapSizeHint;
    private bool _pause;
    private bool _exit;
    private bool _exited;
    private byte[] _heap;
    private long _bytesSerialized;

    // What this worker decided and serialized where, logged during the drain and consumed by
    // WriteSnapshot on the background writer thread. Cleared once the snapshot is on disk.
    private readonly List<SerializedSegment> _segments = [];
    private readonly List<int> _statuses = [];
    private readonly List<IGenericSerializable> _bufferEntities = [];

    internal List<SerializedSegment> Segments => _segments;

    /// <summary>One <see cref="SlotStatus"/> per occupied slot, in drain order.</summary>
    internal List<int> Statuses => _statuses;
    internal List<IGenericSerializable> BufferEntities => _bufferEntities;

    /// <summary>Picks the clean entities a delta save re-serializes for verification.</summary>
    internal SaveSampler Sampler { get; } = new();

    // Per-save decision counters, summed into the SaveReport by the loop after the write.
    internal long EntitiesSerialized;
    internal long EntitiesVerified;
    internal long EntitiesCopied;
    internal long EntitiesSkipped;

    /// <summary>
    /// First serializer exception during the drain. The drain continues so the handshake
    /// completes; the loop fails the save once every worker has paused.
    /// </summary>
    public Exception Error { get; private set; }

    /// <summary>
    /// Releases the write logs after the snapshot is written so serialized entity
    /// references don't linger between saves. Capacity is retained: the logs regrow to
    /// the same size every save.
    /// </summary>
    internal void ReleaseWriteLogs()
    {
        _segments.Clear();
        _statuses.Clear();
        _bufferEntities.Clear();
    }

    private void BeginDrain()
    {
        ReleaseWriteLogs();
        Error = null;
        EntitiesSerialized = 0;
        EntitiesVerified = 0;
        EntitiesCopied = 0;
        EntitiesSkipped = 0;
        Sampler.Reseed(DeltaSaves.Plan.SaveIndex, _index);
    }

    public SerializationThreadWorker(int index, SerializationChunkSource chunkSource, int heapSizeHint = 0)
        : this(index, chunkSource, heapSizeHint, inline: false)
    {
    }

    private SerializationThreadWorker(int index, SerializationChunkSource chunkSource, int heapSizeHint, bool inline)
    {
        _index = index;
        _chunkSource = chunkSource;
        _heapSizeHint = heapSizeHint;

        if (!inline)
        {
            _startEvent = new AutoResetEvent(false);
            _stopEvent = new AutoResetEvent(false);
            _thread = new Thread(Execute);
            _thread.Start(this);
        }
    }

    /// <summary>
    /// Creates a worker with no thread of its own. The owner drains chunks inline via
    /// <see cref="DrainInline"/> — used by the main thread to join the drain instead of
    /// idling while the thread workers finish.
    /// </summary>
    public static SerializationThreadWorker CreateInline(int index, SerializationChunkSource chunkSource, int heapSizeHint = 0) =>
        new(index, chunkSource, heapSizeHint, inline: true);

    // Bytes this worker serialized during the most recent save.
    public long BytesSerialized => _bytesSerialized;

    /// <summary>Records the worker actually serialized (emitted or verify) in the most recent save.</summary>
    public long RecordsSerialized => EntitiesSerialized;

    public void Wake()
    {
        _startEvent.Set();
    }

    public void Sleep()
    {
        Volatile.Write(ref _pause, true);
        _stopEvent.WaitOne();
    }

    public void Exit()
    {
        if (_exited)
        {
            return;
        }

        _exited = true;
        _exit = true;
        Wake();
        Sleep();
    }

    // Sized from the previous world load so the first save doesn't pay copy-on-grow during the freeze.
    public void AllocateHeap() => AllocateHeap(_heapSizeHint);

    /// <summary>
    /// Ensures a heap of about <paramref name="sizeHint"/> bytes before the freeze: allocates
    /// when there is none or it is too small, and releases an oversized one (a delta save after
    /// a full save) so the memory held between saves tracks churn instead of world size.
    /// </summary>
    public void AllocateHeap(int sizeHint)
    {
        var size = Math.Max(MinHeapSize, sizeHint);

        if (_heap == null || _heap.Length < size || _heap.Length > (long)size * 4)
        {
            _heap = null;
            _heap = GC.AllocateUninitializedArray<byte>(size);
        }
    }

    public int HeapSize => _heap?.Length ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetHeap(int start, int length) => _heap.AsSpan(start, length);

    private long ProcessChunk(in SerializationChunkSource.Chunk chunk, BufferWriter writer)
    {
        try
        {
            return ProcessChunkCore(in chunk, writer);
        }
        catch (Exception ex)
        {
            Error ??= ex;

            if (chunk.Buffer != null)
            {
                _chunkSource.Return(chunk.Buffer, chunk.Count);
            }

            return 0;
        }
    }

    private long ProcessChunkCore(in SerializationChunkSource.Chunk chunk, BufferWriter writer)
    {
        if (chunk.Single != null)
        {
            // Self-payloads are written to their own file, so they record placement on the
            // persistence itself instead of the segment logs.
            var start = writer.Position;
            chunk.Single.Serialize(writer);
            chunk.Single.SetSelfPlacement((byte)_index, (int)start, (int)(writer.Position - start));
            EntitiesSerialized++;
            return 1;
        }

        if (chunk.Source != null)
        {
            var heapStart = writer.Position;
            var statusStart = _statuses.Count;
            var occupied = chunk.Source.SerializeRange(this, writer, chunk.Offset, chunk.Count);

            if (occupied > 0)
            {
                _segments.Add(
                    new SerializedSegment(chunk.Source, chunk.Offset, chunk.Count, heapStart, statusStart, occupied, -1)
                );
            }

            return occupied;
        }

        var buffer = chunk.Buffer;
        var count = chunk.Count;
        var plan = DeltaSaves.Plan;

        var bufferHeapStart = writer.Position;
        var bufferStatusStart = _statuses.Count;
        var entitiesStart = _bufferEntities.Count;

        for (var i = 0; i < count; i++)
        {
            var e = buffer[i];

            if (e is ISerializable entity)
            {
                _statuses.Add(DeltaSaves.SerializeEntity(entity, chunk.Owner, this, writer, plan));
            }
            else
            {
                var start = writer.Position;
                e.Serialize(writer);
                _statuses.Add(SlotStatus.Emitted((int)(writer.Position - start)));
                EntitiesSerialized++;
            }

            _bufferEntities.Add(e);
        }

        _segments.Add(
            new SerializedSegment(chunk.Owner, -1, 0, bufferHeapStart, bufferStatusStart, count, entitiesStart)
        );

        _chunkSource.Return(buffer, count);
        return count;
    }

    /// <summary>
    /// Drains chunks on the calling thread until the queue is empty, then returns.
    /// Only valid on inline workers; the main thread calls this after publishing all work
    /// so it contributes drain throughput instead of idling.
    /// </summary>
    public void DrainInline()
    {
        BeginDrain();

        var writer = new BufferWriter(_heap, true);

        while (_chunkSource.TryTake(out var chunk))
        {
            ProcessChunk(in chunk, writer);
        }

        _heap = writer.Buffer;
        _bytesSerialized = writer.Position;

        writer.Close();
    }

    private static void Execute(object obj)
    {
        var worker = (SerializationThreadWorker)obj;

        var chunkSource = worker._chunkSource;

        while (worker._startEvent.WaitOne())
        {
            worker.BeginDrain();

            var writer = new BufferWriter(worker._heap, true);
            var spinner = new SpinWait();

            while (true)
            {
                var pauseRequested = Volatile.Read(ref worker._pause);
                if (chunkSource.TryTake(out var chunk))
                {
                    spinner.Reset();
                    worker.ProcessChunk(in chunk, writer);
                }
                else if (pauseRequested) // Break when finished
                {
                    break;
                }
                else
                {
                    // Idle backoff instead of hammering the queue head while the producer works.
                    // sleep1Threshold: -1 keeps escalation at Yield/Sleep(0) and never Sleep(1),
                    // avoiding timer-resolution stalls at the end of the drain.
                    spinner.SpinOnce(-1);
                }
            }

            worker._heap = writer.Buffer;
            worker._bytesSerialized = writer.Position;

            writer.Close();

            // The owning thread may start another pause cycle the moment _stopEvent is set
            // (Exit does exactly that). Clear _pause and sample the exit condition before
            // signaling, or the new cycle's pause request is clobbered / its Sleep orphaned.
            var exiting = Core.Closing || worker._exit;
            Volatile.Write(ref worker._pause, false);

            worker._stopEvent.Set(); // Allow the main thread to continue now that we are finished

            if (exiting)
            {
                return;
            }
        }
    }
}
