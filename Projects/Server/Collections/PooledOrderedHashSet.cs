/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PooledOrderedHashSet.cs                                         *
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Collections.Extensions;
using Server.Buffers;

namespace Server.Collections;

[DebuggerDisplay("Count = {Count}")]
public class PooledOrderedHashSet<TValue> : IList<TValue>, IDisposable
{
    private struct Entry
    {
        public uint HashCode;
        public TValue Value;
        public int Next; // the index of the next item in the same bucket, -1 if last
    }

    private static readonly Entry[] InitialEntries = new Entry[1];
    private int[] _buckets = HashHelpers.SizeOneIntArray;
    private int _bucketsLength = 1;
    private Entry[] _entries = InitialEntries;
    private int _entriesLength = 1;
    private ulong _fastModMultiplier;
    private int _count;
    private int _version;
#nullable enable
    private readonly IEqualityComparer<TValue>? _comparer;
#nullable disable

    public int Count => _count;
#nullable enable
    public IEqualityComparer<TValue>? Comparer => _comparer;
#nullable disable

    public PooledOrderedHashSet()
        : this(0)
    {
    }

    public PooledOrderedHashSet(IEqualityComparer<TValue> comparer)
        : this(0, comparer)
    {
    }

    public PooledOrderedHashSet(int capacity, IEqualityComparer<TValue> comparer = null)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (capacity > 0)
        {
            int newSize = HashHelpers.GetPrime(capacity);
            _buckets = STArrayPool<int>.Shared.Rent(newSize);
            _bucketsLength = newSize;
            _entries = STArrayPool<Entry>.Shared.Rent(newSize);
            _entriesLength = newSize;
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
        }

        if (comparer != EqualityComparer<TValue>.Default)
        {
            _comparer = comparer;
        }
    }

    public PooledOrderedHashSet(IEnumerable<TValue> collection, IEqualityComparer<TValue> comparer = null)
        : this((collection as ICollection<TValue>)?.Count ?? 0, comparer)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        foreach (TValue value in collection)
        {
            Add(value);
        }
    }

    public bool Contains(TValue item) => TryGetValue(item, out var value) && EqualityComparer<TValue>.Default.Equals(value);

    public void Clear()
    {
        if (_count > 0)
        {
            Array.Clear(_buckets, 0, _bucketsLength);
            Array.Clear(_entries, 0, _count);
            _count = 0;
            ++_version;
        }
    }

    public Enumerator GetEnumerator() => new(this);

    void ICollection<TValue>.Add(TValue item) => TryAdd(item);

    public bool Add(TValue item) => TryAdd(item);

    public int GetOrAdd(TValue value) => TryInsert(null, value);

    public int IndexOf(TValue value) => IndexOf(value, out _);

    public void Insert(int index, TValue value)
    {
        if ((uint)index > (uint)Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), CollectionThrowStrings.ArgumentOutOfRange_Index);
        }

        TryInsert(index, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref int GetBucketRef(uint hashCode)
    {
        int[] buckets = _buckets!;
        return ref buckets[HashHelpers.FastMod(hashCode, (uint)_bucketsLength, _fastModMultiplier)];
    }

    public bool Remove(TValue value)
    {
        int index = IndexOf(value);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        int count = Count;
        if ((uint)index >= (uint)count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), CollectionThrowStrings.ArgumentOutOfRange_Index);
        }

        // Remove the entry from the bucket
        RemoveEntryFromBucket(index);

        // Decrement the indices > index
        Entry[] entries = _entries;
        for (int i = index + 1; i < count; ++i)
        {
            entries[i - 1] = entries[i];
            UpdateBucketIndex(i, incrementAmount: -1);
        }
        --_count;
        entries[_count] = default;
        ++_version;
    }

    public bool TryAdd(TValue value) => TryInsert(null, value) != _count - 1;

    public bool TryGetValue(TValue value, out TValue actualValue)
    {
        int index = IndexOf(value);
        if (index >= 0)
        {
            actualValue = _entries[index].Value;
            return true;
        }

        actualValue = default;
        return false;
    }

    public TValue this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CollectionThrowStrings.ArgumentOutOfRange_Index);
            }

            return _entries[index].Value;
        }
        set
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), CollectionThrowStrings.ArgumentOutOfRange_Index);
            }

            TValue v = value;
            int foundIndex = IndexOf(v, out uint hashCode);
            if (foundIndex < 0)
            {
                RemoveEntryFromBucket(index);
                Entry entry = new Entry { HashCode = hashCode, Value = value };
                AddEntryToBucket(ref entry, index, _buckets, _bucketsLength);
                _entries[index] = entry;
                ++_version;
            }
            else if (foundIndex == index)
            {
                ref Entry entry = ref _entries[index];
                entry.Value = value;
            }
            else
            {
                throw new ArgumentException(string.Format(CollectionThrowStrings.Argument_AddingDuplicate, v.ToString()));
            }
        }
    }

    public bool IsReadOnly => false;

    IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void CopyTo(TValue[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if ((uint)arrayIndex > (uint)array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), CollectionThrowStrings.ArgumentOutOfRange_NeedNonNegNum);
        }

        int count = Count;
        if (array.Length - arrayIndex < count)
        {
            throw new ArgumentException(CollectionThrowStrings.Arg_ArrayPlusOffTooSmall);
        }

        Entry[] entries = _entries;
        for (int i = 0; i < count; ++i)
        {
            Entry entry = entries[i];
            array[i + arrayIndex] = entry.Value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Entry[] Resize(int newSize)
    {
        int[] newBuckets = _buckets.Length < newSize ? STArrayPool<int>.Shared.Rent(newSize) : _buckets;
        Entry[] newEntries = _entries.Length < newSize ? STArrayPool<Entry>.Shared.Rent(newSize) : _entries;

        int count = Count;
        Array.Copy(_entries, newEntries, count);

        _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);

        for (int i = 0; i < count; ++i)
        {
            AddEntryToBucket(ref newEntries[i], i, newBuckets, newSize);
        }

        var oldBuckets = _buckets;
        var oldEntries = _entries;

        if (oldBuckets.Length > 1 && oldBuckets != newBuckets)
        {
            STArrayPool<int>.Shared.Return(oldBuckets, true);
        }

        if (oldEntries.Length > 1 && oldEntries != newEntries)
        {
            STArrayPool<Entry>.Shared.Return(oldEntries, true);
        }

        _buckets = newBuckets;
        _bucketsLength = newSize;
        _entries = newEntries;
        _entriesLength = newSize;
        return newEntries;
    }

#nullable enable
    private int IndexOf(TValue value, out uint hashCode)
    {
        ref int bucket = ref Unsafe.NullRef<int>();
        int i;

        IEqualityComparer<TValue>? comparer = _comparer;
        if (comparer == null)
        {
            hashCode = (uint)value.GetHashCode();
            bucket = ref GetBucketRef(hashCode);
            i = bucket - 1;

            if (i >= 0)
            {
                if (typeof(TValue).IsValueType)
                {
                    // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                    Entry[] entries = _entries;
                    int collisionCount = 0;
                    do
                    {
                        Entry entry = entries[i];
                        if (entry.HashCode == hashCode && EqualityComparer<TValue>.Default.Equals(entry.Value, value))
                        {
                            break;
                        }

                        i = entry.Next;
                        if (collisionCount >= _entriesLength)
                        {
                            // The chain of entries forms a loop; which means a concurrent update has happened.
                            // Break out of the loop and throw, rather than looping forever.
                            throw new InvalidOperationException(
                                CollectionThrowStrings.InvalidOperation_ConcurrentOperationsNotSupported
                            );
                        }

                        ++collisionCount;
                    } while (i >= 0);
                }
                else
                {
                    // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize (https://github.com/dotnet/runtime/issues/10050),
                    // so cache in a local rather than get EqualityComparer per loop iteration.
                    var defaultComparer = EqualityComparer<TValue>.Default;
                    Entry[] entries = _entries;
                    int collisionCount = 0;
                    do
                    {
                        Entry entry = entries[i];
                        if (entry.HashCode == hashCode && defaultComparer.Equals(entry.Value, value))
                        {
                            break;
                        }

                        i = entry.Next;
                        if (collisionCount >= _entriesLength)
                        {
                            // The chain of entries forms a loop; which means a concurrent update has happened.
                            // Break out of the loop and throw, rather than looping forever.
                            throw new InvalidOperationException(
                                CollectionThrowStrings.InvalidOperation_ConcurrentOperationsNotSupported
                            );
                        }

                        ++collisionCount;
                    } while (i >= 0);
                }
            }
        }
        else
        {
            hashCode = (uint)comparer.GetHashCode(value);
            bucket = ref GetBucketRef(hashCode);
            i = bucket - 1;
            if (i >= 0)
            {
                Entry[] entries = _entries;
                int collisionCount = 0;
                do
                {
                    Entry entry = entries[i];
                    if (entry.HashCode == hashCode && comparer.Equals(entry.Value, value))
                    {
                        break;
                    }
                    i = entry.Next;
                    if (collisionCount >= _entriesLength)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_ConcurrentOperationsNotSupported);
                    }
                    ++collisionCount;
                } while (i >= 0);
            }
        }

        return i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int TryInsert(int? index, TValue value)
    {
        int i = IndexOf(value, out uint hashCode);
        return i >= 0 ? i : AddInternal(index, value, hashCode);
    }

    private int AddInternal(int? index, TValue value, uint hashCode)
    {
        Entry[] entries = _entries;
        // Check if resize is needed
        int count = Count;
        if (_entriesLength == count || entries.Length == 1)
        {
            entries = Resize(HashHelpers.ExpandPrime(_entriesLength));
        }

        // Increment indices >= index;
        int actualIndex = index ?? count;
        for (int i = count - 1; i >= actualIndex; --i)
        {
            entries[i + 1] = entries[i];
            UpdateBucketIndex(i, incrementAmount: 1);
        }

        ref Entry entry = ref entries[actualIndex];
        entry.HashCode = hashCode;
        entry.Value = value;
        AddEntryToBucket(ref entry, actualIndex, _buckets, _bucketsLength);
        ++_count;
        ++_version;
        return actualIndex;
    }
#nullable restore

    // Returns the index of the next entry in the bucket
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddEntryToBucket(ref Entry entry, int entryIndex, int[] buckets, int bucketsLength)
    {
        ref int b = ref buckets[(int)(entry.HashCode % (uint)bucketsLength)];
        entry.Next = b - 1;
        b = entryIndex + 1;
    }

    private void RemoveEntryFromBucket(int entryIndex)
    {
        Entry[] entries = _entries;
        Entry entry = entries[entryIndex];
        ref int bucket = ref GetBucketRef(entry.HashCode);
        // Bucket was pointing to removed entry. Update it to point to the next in the chain
        if (bucket == entryIndex + 1)
        {
            bucket = entry.Next + 1;
        }
        else
        {
            // Start at the entry the bucket points to, and walk the chain until we find the entry with the index we want to remove, then fix the chain
            int i = bucket - 1;
            int collisionCount = 0;
            while (true)
            {
                ref Entry e = ref entries[i];
                if (e.Next == entryIndex)
                {
                    e.Next = entry.Next;
                    return;
                }
                i = e.Next;
                if (collisionCount >= _entriesLength)
                {
                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_ConcurrentOperationsNotSupported);
                }
                ++collisionCount;
            }
        }
    }

    private void UpdateBucketIndex(int entryIndex, int incrementAmount)
    {
        Entry[] entries = _entries;
        Entry entry = entries[entryIndex];
        ref int bucket = ref GetBucketRef(entry.HashCode);
        // Bucket was pointing to entry. Increment the index by incrementAmount.
        if (bucket == entryIndex + 1)
        {
            bucket += incrementAmount;
        }
        else
        {
            // Start at the entry the bucket points to, and walk the chain until we find the entry with the index we want to increment.
            int i = bucket - 1;
            int collisionCount = 0;
            while (true)
            {
                ref Entry e = ref entries[i];
                if (e.Next == entryIndex)
                {
                    e.Next += incrementAmount;
                    return;
                }
                i = e.Next;
                if (collisionCount >= _entriesLength)
                {
                    // The chain of entries forms a loop; which means a concurrent update has happened.
                    // Break out of the loop and throw, rather than looping forever.
                    throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_ConcurrentOperationsNotSupported);
                }
                ++collisionCount;
            }
        }
    }

    public struct Enumerator : IEnumerator<TValue>
    {
        private readonly PooledOrderedHashSet<TValue> _PooledOrderedHashSet;
        private readonly int _version;
        private int _index;
        private TValue _current;

        public TValue Current => _current;

        object IEnumerator.Current => _current;

        internal Enumerator(PooledOrderedHashSet<TValue> PooledOrderedHashSet)
        {
            _PooledOrderedHashSet = PooledOrderedHashSet;
            _version = PooledOrderedHashSet._version;
            _index = 0;
            _current = default;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_version != _PooledOrderedHashSet._version)
            {
                throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
            }

            if (_index < _PooledOrderedHashSet.Count)
            {
                Entry entry = _PooledOrderedHashSet._entries[_index];
                _current = entry.Value;
                ++_index;
                return true;
            }
            _current = default;
            return false;
        }

        void IEnumerator.Reset()
        {
            if (_version != _PooledOrderedHashSet._version)
            {
                throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
            }

            _index = 0;
            _current = default;
        }
    }

    public void Dispose()
    {
        if (_buckets.Length > 1)
        {
            STArrayPool<int>.Shared.Return(_buckets, true);
        }

        if (_entries.Length > 1)
        {
            STArrayPool<Entry>.Shared.Return(_entries, true);
        }

        _buckets = HashHelpers.SizeOneIntArray;
        _entries = InitialEntries;
        _count = 0;

        GC.SuppressFinalize(this);
    }

    ~PooledOrderedHashSet()
    {
        if (_buckets.Length > 1)
        {
            STArrayPool<int>.Shared.Return(_buckets, true);
        }

        if (_entries.Length > 1)
        {
            STArrayPool<Entry>.Shared.Return(_entries, true);
        }

        _buckets = HashHelpers.SizeOneIntArray;
        _entries = InitialEntries;
        _count = 0;
    }
}
