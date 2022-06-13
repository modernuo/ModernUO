// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Server.Buffers;

/**
 * Adaptation of the ArrayPool<T>.Shared (TlsOverPerCoreLockedStacksArrayPool) for single threaded *unsafe* usage.
 */
public class STArrayPool<T> : ArrayPool<T>
{
    private const int StackArraySize = 8;
    private const int BucketCount = 27; // SelectBucketIndex(1024 * 1024 * 1024 + 1)
    private static readonly STArrayPool<T> _shared = new();

    public static STArrayPool<T> Shared => _shared;

    private int _trimCallbackCreated;
    private static STArray[] _cacheBuckets;
    private STArrayStack[] _buckets = new STArrayStack[BucketCount];

    private STArrayPool() {}

    public override T[] Rent(int minimumLength)
    {
        T[] buffer;

        var bucketIndex = SelectBucketIndex(minimumLength);
        var cachedBuckets = _cacheBuckets;
        if (cachedBuckets is not null && (uint)bucketIndex < (uint)cachedBuckets.Length)
        {
            buffer = cachedBuckets[bucketIndex].Array;
            if (buffer is not null)
            {
                cachedBuckets[bucketIndex].Array = null;
                return buffer;
            }
        }

        var buckets = _buckets;
        if ((uint)bucketIndex < (uint)buckets.Length)
        {
            var b = buckets[bucketIndex];
            if (b is not null)
            {
                buffer = b.TryPop();
                if (buffer is not null)
                {
                    return buffer;
                }
            }

            minimumLength = GetMaxSizeForBucket(bucketIndex);
        }

        if (minimumLength == 0)
        {
            // We aren't renting.
            return Array.Empty<T>();
        }

        if (minimumLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumLength));
        }

        buffer = GC.AllocateUninitializedArray<T>(minimumLength);
        return buffer;
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        if (array is null)
        {
            return;
        }

        var bucketIndex = SelectBucketIndex(array.Length);
        var cacheBuckets = _cacheBuckets ?? InitializeBuckets();

        if ((uint)bucketIndex < (uint)_cacheBuckets!.Length)
        {
            if (clearArray)
            {
                Array.Clear(array);
            }

            if (array.Length != GetMaxSizeForBucket(bucketIndex))
            {
                throw new ArgumentException("Buffer is not from the pool", nameof(array));
            }

            ref var bucketArray = ref cacheBuckets[bucketIndex];
            var prev = bucketArray.Array;
            bucketArray = new STArray(array);
            if (prev is not null)
            {
                var bucket = _buckets[bucketIndex] ?? CreateBucketStack(bucketIndex);
                bucket.TryPush(prev);
            }
        }
    }

    public void ResetForTesting()
    {
        if (Core.IsRunningFromXUnit)
        {
            _cacheBuckets = null;
            _buckets = new STArrayStack[BucketCount];
        }
    }

    public bool Trim()
    {
        var ticks = Core.TickCount;
        var pressure = GetMemoryPressure();

        var buckets = _buckets;
        for (var i = 0; i < buckets.Length; i++)
        {
            buckets[i]?.Trim(ticks, pressure, GetMaxSizeForBucket(i));
        }

        if (_cacheBuckets == null)
        {
            return true;
        }

        // Under high pressure, release all cached buckets
        if (pressure == MemoryPressure.High)
        {
            Array.Clear(_cacheBuckets);
        }
        else
        {
            uint threshold = pressure switch
            {
                MemoryPressure.Medium => 10000,
                _                     => 30000,
            };

            var cacheBuckets = _cacheBuckets;
            for (var i = 0; i < cacheBuckets.Length; i++)
            {
                ref var b = ref cacheBuckets[i];

                if (b.Array is null)
                {
                    continue;
                }

                var lastSeen = b.Ticks;
                if (lastSeen == 0)
                {
                    b.Ticks = ticks;
                }
                else if (ticks - lastSeen >= threshold)
                {
                    b.Array = null;
                }
            }
        }

        return true;
    }

    private STArrayStack CreateBucketStack(int bucketIndex)
    {
        return _buckets[bucketIndex] = new STArrayStack();
    }

    private STArray[] InitializeBuckets()
    {
        Debug.Assert(_cacheBuckets is null, $"Non-null {nameof(_cacheBuckets)}");
        var buckets = new STArray[BucketCount];

        if (Interlocked.Exchange(ref _trimCallbackCreated, 1) == 0)
        {
            Gen2GcCallback.Register(o => ((STArrayPool<T>)o).Trim(), this);
        }

        return _cacheBuckets = buckets;
    }

    // Buffers are bucketed so that a request between 2^(n-1) + 1 and 2^n is given a buffer of 2^n
    // Bucket index is log2(bufferSize - 1) with the exception that buffers between 1 and 16 bytes
    // are combined, and the index is slid down by 3 to compensate.
    // Zero is a valid bufferSize, and it is assigned the highest bucket index so that zero-length
    // buffers are not retained by the pool. The pool will return the Array.Empty singleton for these.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int SelectBucketIndex(int bufferSize) => BitOperations.Log2((uint)bufferSize - 1 | 15) - 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetMaxSizeForBucket(int binIndex)
    {
        int maxSize = 16 << binIndex;
        Debug.Assert(maxSize >= 0);
        return maxSize;
    }

    internal enum MemoryPressure
    {
        Low,
        Medium,
        High
    }

    internal static MemoryPressure GetMemoryPressure()
    {
        GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();

        if (memoryInfo.MemoryLoadBytes >= memoryInfo.HighMemoryLoadThresholdBytes * 0.90)
        {
            return MemoryPressure.High;
        }

        if (memoryInfo.MemoryLoadBytes >= memoryInfo.HighMemoryLoadThresholdBytes * 0.70)
        {
            return MemoryPressure.Medium;
        }

        return MemoryPressure.Low;
    }

    private sealed class STArrayStack
    {
        // Maximum buffers we will store in our stack
        private readonly T[][] _arrays = new T[StackArraySize * Environment.ProcessorCount][];
        private int _count;
        private long _ticks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPush(T[] array)
        {
            var arrays = _arrays;
            var count = _count;
            if ((uint)count < (uint)_arrays.Length)
            {
                arrays[count] = array;
                _count = count + 1;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] TryPop()
        {
            var arrays = _arrays;
            var count = _count - 1;
            if ((uint)count < (uint)arrays.Length)
            {
                var arr = arrays[count];
                arrays[count] = null;
                _count = count;
                return arr;
            }

            return null;
        }

        public void Trim(long now, MemoryPressure pressure, int bucketSize)
        {
            if (_count == 0)
            {
                return;
            }

            // 10 seconds under high pressure, otherwise 60 seconds
            var threshold = pressure == MemoryPressure.High ? 10000 : 60000;

            if (_ticks == 0)
            {
                _ticks = now;
                return;
            }

            if (now - _ticks <= threshold)
            {
                return;
            }

            int trimCount = 1;
            switch (pressure)
            {
                case MemoryPressure.Medium:
                    {
                        trimCount = 2;
                        break;
                    }
                case MemoryPressure.High:
                    {
                        if (bucketSize > 16384)
                        {
                            trimCount++;
                        }

                        var size = Unsafe.SizeOf<T>();
                        if (size > 32)
                        {
                            trimCount += 2;
                        }
                        else if (size > 16)
                        {
                            trimCount++;
                        }

                        break;
                    }
            }

            while (_count > 0 && trimCount-- > 0)
            {
                _arrays[--_count] = null;
            }
        }
    }

    private struct STArray
    {
        public T[] Array;
        public long Ticks;

        public STArray(T[] array)
        {
            Array = array;
            Ticks = 0;
        }
    }
}
