// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.CompilerServices;

namespace System.Network;

/// <summary>
/// A multi-slab pool of pre-allocated IORingBuffer instances for zero-allocation buffer management.
/// </summary>
/// <remarks>
/// <para>
/// Buffers are organized into slabs that are allocated on-demand. Each slab contains a fixed
/// number of buffers that are pre-registered with the ring when the slab is created.
/// </para>
/// <para>
/// Memory growth model:
/// - Start with <c>initialSlabs</c> worth of buffers
/// - Grow by adding slabs as demand increases
/// - Cap at <c>maxSlabs</c> to prevent OOM
/// - Fall back to individual allocations beyond max (with stats tracking)
/// </para>
/// <para>
/// Use <see cref="GetStats"/> to monitor pool health and detect when more slabs are needed.
/// </para>
/// </remarks>
public sealed class IORingBufferPool : IDisposable
{
    private readonly IIORingGroup _ring;
    private readonly List<PoolSlab> _slabs;
    private int _firstNonFullSlab;
    private bool _disposed;

    // Fallback tracking
    private int _fallbackAllocations;
    private int _currentFallbackCount;
    private int _peakFallbackCount;

    /// <summary>
    /// Represents a slab of pre-allocated buffers.
    /// </summary>
    private sealed class PoolSlab
    {
        public readonly IORingBuffer[] Buffers;
        public readonly int[] FreeStack;
        public int FreeCount;

        public PoolSlab(int size)
        {
            Buffers = new IORingBuffer[size];
            FreeStack = new int[size];
            FreeCount = 0;
        }
    }

    /// <summary>
    /// Gets the number of buffers per slab.
    /// </summary>
    public int SlabSize { get; }

    /// <summary>
    /// Gets the physical size of each buffer in bytes.
    /// </summary>
    public int BufferSize { get; }

    /// <summary>
    /// Gets the maximum number of slabs allowed.
    /// </summary>
    public int MaxSlabs { get; }

    /// <summary>
    /// Gets the current number of allocated slabs.
    /// </summary>
    public int CurrentSlabs => _slabs.Count;

    /// <summary>
    /// Gets the total capacity (current slabs × slab size).
    /// </summary>
    public int TotalCapacity => CurrentSlabs * SlabSize;

    /// <summary>
    /// Creates a buffer pool with on-demand slab allocation.
    /// </summary>
    /// <param name="ring">The ring to register buffers with.</param>
    /// <param name="slabSize">Number of buffers per slab (e.g., 64 or 256).</param>
    /// <param name="bufferSize">Physical size of each buffer (must be power of 2, page-aligned).</param>
    /// <param name="initialSlabs">Number of slabs to pre-allocate (default: 1).</param>
    /// <param name="maxSlabs">Maximum number of slabs allowed (default: 16).</param>
    /// <exception cref="ArgumentNullException">If ring is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If sizes are invalid.</exception>
    public IORingBufferPool(
        IIORingGroup ring,
        int slabSize,
        int bufferSize,
        int initialSlabs = 1,
        int maxSlabs = 16)
    {
        _ring = ring ?? throw new ArgumentNullException(nameof(ring));

        if (slabSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slabSize), "Slab size must be positive");
        }

        if (bufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be positive");
        }

        if (initialSlabs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialSlabs), "Initial slabs cannot be negative");
        }

        if (maxSlabs < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSlabs), "Max slabs must be at least 1");
        }

        if (initialSlabs > maxSlabs)
        {
            throw new ArgumentOutOfRangeException(nameof(initialSlabs), "Initial slabs cannot exceed max slabs");
        }

        SlabSize = slabSize;
        BufferSize = bufferSize;
        MaxSlabs = maxSlabs;
        _slabs = new List<PoolSlab>(maxSlabs);
        _firstNonFullSlab = 0;

        // Pre-allocate initial slabs
        for (var i = 0; i < initialSlabs; i++)
        {
            _slabs.Add(CreateSlab(i));
        }
    }

    /// <summary>
    /// Creates and initializes a new slab with all buffers registered.
    /// </summary>
    private PoolSlab CreateSlab(int slabId)
    {
        var slab = new PoolSlab(SlabSize);
        var basePoolIndex = slabId * SlabSize;

        for (var i = 0; i < SlabSize; i++)
        {
            var poolIndex = basePoolIndex + i;
            var buffer = IORingBuffer.Create(BufferSize, isPooled: true, poolIndex: poolIndex);

            // Register with ring
            var bufferId = _ring.RegisterBuffer(buffer);
            buffer.BufferId = bufferId;

            slab.Buffers[i] = buffer;
            slab.FreeStack[i] = i;
        }

        slab.FreeCount = SlabSize;
        return slab;
    }

    /// <summary>
    /// Acquires a buffer from the pool.
    /// If all slabs are full and we haven't hit max, a new slab is allocated.
    /// If at max slabs, a fallback buffer is allocated dynamically.
    /// </summary>
    /// <returns>An IORingBuffer ready for use.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IORingBuffer Acquire()
    {
        // Start from first potentially non-full slab (optimization)
        for (var i = _firstNonFullSlab; i < _slabs.Count; i++)
        {
            var slab = _slabs[i];
            if (slab.FreeCount > 0)
            {
                _firstNonFullSlab = i;
                return AcquireFromSlab(slab);
            }
        }

        // At max slabs - create fallback buffer
        if (_slabs.Count >= MaxSlabs)
        {
            return CreateFallbackBuffer();
        }

        var newSlab = CreateSlab(_slabs.Count);
        _slabs.Add(newSlab);
        _firstNonFullSlab = _slabs.Count - 1;
        return AcquireFromSlab(newSlab);
    }

    /// <summary>
    /// Acquires a buffer from a specific slab.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IORingBuffer AcquireFromSlab(PoolSlab slab)
    {
        var slotIndex = slab.FreeStack[--slab.FreeCount];
        var buffer = slab.Buffers[slotIndex];
        buffer.Reset();
        return buffer;
    }

    /// <summary>
    /// Tries to acquire a buffer from the pool without fallback allocation.
    /// </summary>
    /// <param name="buffer">The acquired buffer, or null if pool is exhausted.</param>
    /// <returns>True if a buffer was acquired, false if pool is at max capacity.</returns>
    public bool TryAcquire(out IORingBuffer? buffer)
    {
        // Try existing slabs
        for (var i = _firstNonFullSlab; i < _slabs.Count; i++)
        {
            var slab = _slabs[i];
            if (slab.FreeCount > 0)
            {
                _firstNonFullSlab = i;
                buffer = AcquireFromSlab(slab);
                return true;
            }
        }

        // Try to create new slab
        if (_slabs.Count < MaxSlabs)
        {
            var newSlab = CreateSlab(_slabs.Count);
            _slabs.Add(newSlab);
            _firstNonFullSlab = _slabs.Count - 1;
            buffer = AcquireFromSlab(newSlab);
            return true;
        }

        buffer = null;
        return false;
    }

    /// <summary>
    /// Releases a buffer back to the pool or disposes it if it's a fallback buffer.
    /// </summary>
    /// <param name="buffer">The buffer to release.</param>
    public void Release(IORingBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer.IsPooled)
        {
            // Decode slab and slot from PoolIndex
            var slabId = buffer.PoolIndex / SlabSize;
            var slotIndex = buffer.PoolIndex % SlabSize;

            if (slabId < _slabs.Count)
            {
                var slab = _slabs[slabId];
                slab.FreeStack[slab.FreeCount++] = slotIndex;

                // Update hint if releasing to earlier slab
                if (slabId < _firstNonFullSlab)
                {
                    _firstNonFullSlab = slabId;
                }
            }
        }
        else
        {
            // Fallback buffer - unregister and dispose
            if (buffer.BufferId >= 0)
            {
                _ring.UnregisterBuffer(buffer.BufferId);
            }

            buffer.Dispose();
            _currentFallbackCount--;
        }
    }

    /// <summary>
    /// Creates a fallback buffer when pool is at max capacity.
    /// </summary>
    private IORingBuffer CreateFallbackBuffer()
    {
        _fallbackAllocations++;
        _currentFallbackCount++;

        // Track peak fallback usage
        if (_currentFallbackCount > _peakFallbackCount)
        {
            _peakFallbackCount = _currentFallbackCount;
        }

        var buffer = IORingBuffer.Create(BufferSize, isPooled: false, poolIndex: -1);

        // Register with ring
        var bufferId = _ring.RegisterBuffer(buffer);
        buffer.BufferId = bufferId;

        return buffer;
    }

    /// <summary>
    /// Gets statistics about pool usage.
    /// </summary>
    public SlabPoolStats GetStats()
    {
        var totalCapacity = _slabs.Count * SlabSize;
        var freeCount = 0;
        for (var i = 0; i < _slabs.Count; i++)
        {
            var slab = _slabs[i];
            freeCount += slab.FreeCount;
        }

        return new SlabPoolStats
        {
            SlabSize = SlabSize,
            BufferSize = BufferSize,
            CurrentSlabs = _slabs.Count,
            MaxSlabs = MaxSlabs,
            TotalCapacity = totalCapacity,
            InUseCount = totalCapacity - freeCount,
            FreeCount = freeCount,
            TotalFallbackAllocations = _fallbackAllocations,
            CurrentFallbackCount = _currentFallbackCount,
            PeakFallbackCount = _peakFallbackCount,
            CommittedBytes = (long)_slabs.Count * SlabSize * BufferSize * 2, // ×2 for double-mapping
        };
    }

    /// <summary>
    /// Resets the fallback statistics counters.
    /// </summary>
    public void ResetStats()
    {
        _fallbackAllocations = 0;
        _peakFallbackCount = _currentFallbackCount;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Unregister and dispose all pooled buffers in all slabs
        for (var i = 0; i < _slabs.Count; i++)
        {
            var slab = _slabs[i];
            for (var j = 0; j < slab.Buffers.Length; j++)
            {
                var buffer = slab.Buffers[j];
                if (buffer.BufferId >= 0)
                {
                    _ring.UnregisterBuffer(buffer.BufferId);
                }

                buffer.Dispose();
            }
        }

        _slabs.Clear();
    }
}

/// <summary>
/// Statistics about multi-slab buffer pool usage.
/// </summary>
public readonly struct SlabPoolStats
{
    /// <summary>
    /// Number of buffers per slab.
    /// </summary>
    public int SlabSize { get; init; }

    /// <summary>
    /// Physical size of each buffer in bytes.
    /// </summary>
    public int BufferSize { get; init; }

    /// <summary>
    /// Current number of allocated slabs.
    /// </summary>
    public int CurrentSlabs { get; init; }

    /// <summary>
    /// Maximum number of slabs allowed.
    /// </summary>
    public int MaxSlabs { get; init; }

    /// <summary>
    /// Total buffer capacity (CurrentSlabs × SlabSize).
    /// </summary>
    public int TotalCapacity { get; init; }

    /// <summary>
    /// Number of buffers currently in use.
    /// </summary>
    public int InUseCount { get; init; }

    /// <summary>
    /// Number of buffers available in pool.
    /// </summary>
    public int FreeCount { get; init; }

    /// <summary>
    /// Total number of fallback allocations that occurred (lifetime).
    /// A non-zero value indicates the pool was exhausted at some point.
    /// </summary>
    public int TotalFallbackAllocations { get; init; }

    /// <summary>
    /// Current number of fallback (non-pooled) buffers in use.
    /// </summary>
    public int CurrentFallbackCount { get; init; }

    /// <summary>
    /// Peak number of fallback buffers in use at any one time.
    /// </summary>
    public int PeakFallbackCount { get; init; }

    /// <summary>
    /// Total committed memory in bytes (including double-mapping overhead).
    /// </summary>
    public long CommittedBytes { get; init; }

    /// <summary>
    /// True if any fallback allocations have occurred.
    /// </summary>
    public bool HasFallbacks => TotalFallbackAllocations > 0;

    /// <summary>
    /// True if the pool can grow by adding more slabs.
    /// </summary>
    public bool CanGrow => CurrentSlabs < MaxSlabs;

    /// <summary>
    /// Pool utilization percentage (0-100).
    /// </summary>
    public double UtilizationPercent => TotalCapacity > 0
        ? (double)InUseCount / TotalCapacity * 100
        : 0;

    public override string ToString() =>
        $"Slabs: {CurrentSlabs}/{MaxSlabs}, " +
        $"Buffers: {InUseCount}/{TotalCapacity} ({UtilizationPercent:F1}%), " +
        $"Fallbacks: {CurrentFallbackCount} current, {TotalFallbackAllocations} total" +
        (CanGrow ? " [can grow]" : " [at max]");
}
