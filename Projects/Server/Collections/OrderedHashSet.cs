/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: OrderedHashSet.cs                                               *
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Collections.Extensions;

namespace Server.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public partial class OrderedHashSet<TValue> : IList<TValue>, IReadOnlyList<TValue>, ISet<TValue>, IReadOnlySet<TValue>
    {
        private struct Entry
        {
            public uint HashCode;
            public TValue Value;
            public int Next; // the index of the next item in the same bucket, -1 if last
        }

        private const string ArgumentOutOfRange_Index =
            "Index was out of range. Must be non-negative and less than the size of the collection.";

        private const string ArgumentOutOfRange_NeedNonNegNum =
            "Non-negative number required.";

        private const string Argument_InvalidOffLen =
            "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.";

        private const string Argument_AddingDuplicate = "An item with the same value has already been added. Value: {0}";

        private const string Arg_ArrayPlusOffTooSmall =
            "Destination array is not long enough to copy all the items in the collection. Check array index and length.";

        private const string InvalidOperation_ConcurrentOperationsNotSupported =
            "Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.";

        private const string InvalidOperation_EnumFailedVersion =
            "Collection was modified; enumeration operation may not execute.";

        private static readonly Entry[] InitialEntries = new Entry[1];
        private int[] _buckets = HashHelpers.SizeOneIntArray;
        private Entry[] _entries = InitialEntries;
        private ulong _fastModMultiplier;
        private int _count;
        private int _version;
        private readonly IEqualityComparer<TValue> _comparer;
        private ValueCollection _values;

        public int Count => _count;
        public IEqualityComparer<TValue> Comparer => _comparer ?? EqualityComparer<TValue>.Default;
        public ValueCollection Values => _values ??= new ValueCollection(this);

        public OrderedHashSet()
            : this(0)
        {
        }

        public OrderedHashSet(IEqualityComparer<TValue> comparer)
            : this(0, comparer)
        {
        }

        public OrderedHashSet(int capacity, IEqualityComparer<TValue> comparer = null)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (capacity > 0)
            {
                int newSize = HashHelpers.GetPrime(capacity);
                _buckets = new int[newSize];
                _entries = new Entry[newSize];
                _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
            }

            if (comparer != EqualityComparer<TValue>.Default)
            {
                _comparer = comparer;
            }
        }

        public OrderedHashSet(IEnumerable<TValue> collection, IEqualityComparer<TValue> comparer = null)
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

        public void ExceptWith(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        public void IntersectWith(IEnumerable<TValue> other)
        {
            throw new NotImplementedException();
        }

        public bool Contains(TValue item) => TryGetValue(item, out var value) && EqualityComparer<TValue>.Default.Equals(value);

        // TODO: Implement IReadOnlySet and ISet

        bool IReadOnlySet<TValue>.IsProperSubsetOf(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool IReadOnlySet<TValue>.IsProperSupersetOf(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool IReadOnlySet<TValue>.IsSubsetOf(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool IReadOnlySet<TValue>.IsSupersetOf(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool IReadOnlySet<TValue>.Overlaps(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool IReadOnlySet<TValue>.SetEquals(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool ISet<TValue>.IsProperSubsetOf(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool ISet<TValue>.IsProperSupersetOf(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool ISet<TValue>.IsSubsetOf(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool ISet<TValue>.IsSupersetOf(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool ISet<TValue>.Overlaps(IEnumerable<TValue> other) => throw new NotImplementedException();

        bool ISet<TValue>.SetEquals(IEnumerable<TValue> other) => throw new NotImplementedException();

        public void SymmetricExceptWith(IEnumerable<TValue> other) => throw new NotImplementedException();

        public void UnionWith(IEnumerable<TValue> other) => throw new NotImplementedException();

        public void Clear()
        {
            if (_count > 0)
            {
                Array.Clear(_buckets, 0, _buckets.Length);
                Array.Clear(_entries, 0, _count);
                _count = 0;
                ++_version;
            }
        }

        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            if (_entries.Length >= capacity)
            {
                return _entries.Length;
            }
            int newSize = HashHelpers.GetPrime(capacity);
            Resize(newSize);
            ++_version;
            return newSize;
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
                throw new ArgumentOutOfRangeException(nameof(index), ArgumentOutOfRange_Index);
            }

            TryInsert(index, value);
        }

        public void Move(int fromIndex, int toIndex)
        {
            if ((uint)fromIndex >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(fromIndex), ArgumentOutOfRange_Index);
            }
            if ((uint)toIndex >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(toIndex), ArgumentOutOfRange_Index);
            }

            if (fromIndex == toIndex)
            {
                return;
            }

            Entry[] entries = _entries;
            Entry temp = entries[fromIndex];
            RemoveEntryFromBucket(fromIndex);
            int direction = fromIndex < toIndex ? 1 : -1;
            for (int i = fromIndex; i != toIndex; i += direction)
            {
                entries[i] = entries[i + direction];
                UpdateBucketIndex(i + direction, -direction);
            }
            AddEntryToBucket(ref temp, toIndex, _buckets);
            entries[toIndex] = temp;
            ++_version;
        }

        public void MoveRange(int fromIndex, int toIndex, int count)
        {
            if (count == 1)
            {
                Move(fromIndex, toIndex);
                return;
            }

            if ((uint)fromIndex >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(fromIndex), ArgumentOutOfRange_Index);
            }
            if ((uint)toIndex >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(toIndex), ArgumentOutOfRange_Index);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), ArgumentOutOfRange_NeedNonNegNum);
            }
            if (fromIndex + count > Count)
            {
                throw new ArgumentException(Argument_InvalidOffLen);
            }
            if (toIndex + count > Count)
            {
                throw new ArgumentException(Argument_InvalidOffLen);
            }

            if (fromIndex == toIndex || count == 0)
            {
                return;
            }

            Entry[] entries = _entries;
            Entry[] entriesToMove = ArrayPool<Entry>.Shared.Rent(count);
            for (int i = 0; i < count; ++i)
            {
                entriesToMove[i] = entries[fromIndex + i];
                RemoveEntryFromBucket(fromIndex + i);
            }

            // Move entries in between
            int direction = 1;
            int amount = count;
            int start = fromIndex;
            int end = toIndex;
            if (fromIndex > toIndex)
            {
                direction = -1;
                amount = -count;
                start = fromIndex + count - 1;
                end = toIndex + count - 1;
            }
            for (int i = start; i != end; i += direction)
            {
                entries[i] = entries[i + amount];
                UpdateBucketIndex(i + amount, -amount);
            }

            int[] buckets = _buckets;
            // Copy entries to destination
            for (int i = 0; i < count; ++i)
            {
                Entry temp = entriesToMove[i];
                AddEntryToBucket(ref temp, toIndex + i, buckets);
                entries[toIndex + i] = temp;
            }
            ++_version;
            ArrayPool<Entry>.Shared.Return(entriesToMove);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucketRef(uint hashCode)
        {
            int[] buckets = _buckets!;
            return ref buckets[HashHelpers.FastMod(hashCode, (uint)buckets.Length, _fastModMultiplier)];
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
                throw new ArgumentOutOfRangeException(nameof(index), ArgumentOutOfRange_Index);
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

        public void TrimExcess() => TrimExcess(Count);

        public void TrimExcess(int capacity)
        {
            if (capacity < Count)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            int newSize = HashHelpers.GetPrime(capacity);
            if (newSize < _entries.Length)
            {
                Resize(newSize);
                ++_version;
            }
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
                    throw new ArgumentOutOfRangeException(nameof(index), ArgumentOutOfRange_Index);
                }

                return _entries[index].Value;
            }
            set
            {
                if ((uint)index >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), ArgumentOutOfRange_Index);
                }

                TValue v = value;
                int foundIndex = IndexOf(v, out uint hashCode);
                if (foundIndex < 0)
                {
                    RemoveEntryFromBucket(index);
                    Entry entry = new Entry { HashCode = hashCode, Value = value };
                    AddEntryToBucket(ref entry, index, _buckets);
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
                    throw new ArgumentException(string.Format(Argument_AddingDuplicate, v.ToString()));
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
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), ArgumentOutOfRange_NeedNonNegNum);
            }

            int count = Count;
            if (array.Length - arrayIndex < count)
            {
                throw new ArgumentException(Arg_ArrayPlusOffTooSmall);
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
            int[] newBuckets = new int[newSize];
            Entry[] newEntries = new Entry[newSize];

            int count = Count;
            Array.Copy(_entries, newEntries, count);

            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);

            for (int i = 0; i < count; ++i)
            {
                AddEntryToBucket(ref newEntries[i], i, newBuckets);
            }

            _buckets = newBuckets;
            _entries = newEntries;
            return newEntries;
        }

        private int IndexOf(TValue value, out uint hashCode)
        {
            IEqualityComparer<TValue> comparer = _comparer;
            hashCode = (uint)(comparer?.GetHashCode(value) ?? value?.GetHashCode() ?? 0);
            ref int bucket = ref GetBucketRef(hashCode);
            int i = bucket - 1;
            if (i >= 0)
            {
                comparer ??= EqualityComparer<TValue>.Default;
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
                    if (collisionCount >= entries.Length)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        throw new InvalidOperationException(InvalidOperation_ConcurrentOperationsNotSupported);
                    }
                    ++collisionCount;
                } while (i >= 0);
            }
            return i;
        }

#nullable enable
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
            if (entries.Length == count || entries.Length == 1)
            {
                entries = Resize(HashHelpers.ExpandPrime(entries.Length));
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
            AddEntryToBucket(ref entry, actualIndex, _buckets);
            ++_count;
            ++_version;
            return actualIndex;
        }
#nullable restore

        // Returns the index of the next entry in the bucket
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddEntryToBucket(ref Entry entry, int entryIndex, int[] buckets)
        {
            ref int b = ref buckets[(int)(entry.HashCode % (uint)buckets.Length)];
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
                    if (collisionCount >= entries.Length)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        throw new InvalidOperationException(InvalidOperation_ConcurrentOperationsNotSupported);
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
                    if (collisionCount >= entries.Length)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        throw new InvalidOperationException(InvalidOperation_ConcurrentOperationsNotSupported);
                    }
                    ++collisionCount;
                }
            }
        }

        public struct Enumerator : IEnumerator<TValue>
        {
            private readonly OrderedHashSet<TValue> _orderedHashSet;
            private readonly int _version;
            private int _index;
            private TValue _current;

            public TValue Current => _current;

            object IEnumerator.Current => _current;

            internal Enumerator(OrderedHashSet<TValue> orderedHashSet)
            {
                _orderedHashSet = orderedHashSet;
                _version = orderedHashSet._version;
                _index = 0;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_version != _orderedHashSet._version)
                {
                    throw new InvalidOperationException(InvalidOperation_EnumFailedVersion);
                }

                if (_index < _orderedHashSet.Count)
                {
                    Entry entry = _orderedHashSet._entries[_index];
                    _current = entry.Value;
                    ++_index;
                    return true;
                }
                _current = default;
                return false;
            }

            void IEnumerator.Reset()
            {
                if (_version != _orderedHashSet._version)
                {
                    throw new InvalidOperationException(InvalidOperation_EnumFailedVersion);
                }

                _index = 0;
                _current = default;
            }
        }
    }
}
