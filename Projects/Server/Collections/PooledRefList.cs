// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Server.Buffers;

namespace Server.Collections;

// Implements a variable-size List that uses an array of objects to store the
// elements. A List has a capacity, which is the allocated length
// of the internal array. As elements are added to a List, the capacity
// of the List is automatically increased as required by reallocating the
// internal array.
//
[DebuggerDisplay("Count = {Count}")]
public ref struct PooledRefList<T>
{
    private const int MaxLength = int.MaxValue;
    private const int DefaultCapacity = 4;

    internal T[] _items;  // Do not rename (binary serialization)
    internal int _size;   // Do not rename (binary serialization)
    private int _version; // Do not rename (binary serialization)
    private bool _mt;

#pragma warning disable CA1825 // avoid the extra generic instantiation for Array.Empty<T>()
    private static readonly T[] s_emptyArray = new T[0];
#pragma warning restore CA1825

    private ArrayPool<T> ArrayPool
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _mt ? ArrayPool<T>.Shared : STArrayPool<T>.Shared;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledRefList<T> Create(int capacity = 32, bool mt = false) => new(capacity, mt);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PooledRefList<T> CreateMT(int capacity = 32) => new(capacity, true);

    // Constructs a List. The list is initially empty and has a capacity
    // of zero. Upon adding the first element to the list the capacity is
    // increased to DefaultCapacity, and then increased in multiples of two
    // as required.
    public PooledRefList(int capacity, bool mt = false)
    {
        _mt = mt;
        _size = 0;
        _version = 0;
        _items = capacity switch
        {
            < 0 => throw new ArgumentOutOfRangeException(nameof(capacity), capacity, CollectionThrowStrings.ArgumentOutOfRange_NeedNonNegNum),
            0   => Array.Empty<T>(),
            _   => (_mt ? ArrayPool<T>.Shared : STArrayPool<T>.Shared).Rent(capacity)
        };

    }

    // Constructs a List, copying the contents of the given collection. The
    // size and capacity of the new list will both be equal to the size of the
    // given collection.
    //
    public PooledRefList(PooledRefList<T> collection, bool mt = false)
    {
        _version = 0;
        _mt = mt;

        int count = collection.Count;
        if (count == 0)
        {
            _items = s_emptyArray;
            _size = 0;
        }
        else
        {
            _items = (mt ? ArrayPool<T>.Shared : STArrayPool<T>.Shared).Rent(count);
            collection.CopyTo(_items, 0);
            _size = count;
        }
    }

    // Constructs a List, copying the contents of the given collection. The
    // size and capacity of the new list will both be equal to the size of the
    // given collection.
    //
    public PooledRefList(IEnumerable<T> collection, bool mt = false)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        _version = 0;
        _mt = mt;

        if (collection is ICollection<T> c)
        {
            int count = c.Count;
            if (count == 0)
            {
                _items = s_emptyArray;
                _size = 0;
            }
            else
            {
                _items = (mt ? ArrayPool<T>.Shared : STArrayPool<T>.Shared).Rent(count);
                c.CopyTo(_items, 0);
                _size = count;
            }
        }
        else
        {
            _size = 0;
            _items = s_emptyArray;
            using IEnumerator<T> en = collection!.GetEnumerator();
            while (en.MoveNext())
            {
                Add(en.Current);
            }
        }
    }

    // Gets and sets the capacity of this list.  The capacity is the size of
    // the internal array used to hold items.  When set, the internal
    // array of the list is reallocated to the given capacity.
    //
    public int Capacity
    {
        get => _items.Length;
        set
        {
            if (value < _size)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value != _items.Length)
            {
                if (value > 0)
                {
                    T[] newItems = ArrayPool.Rent(_size);
                    if (_size > 0)
                    {
                        Array.Copy(_items, newItems, _size);
                    }

                    if (_items.Length > 0)
                    {
                        Clear();
                        ArrayPool.Return(_items);
                    }
                    _items = newItems;
                }
                else
                {
                    Clear();
                    ArrayPool.Return(_items);
                    _items = s_emptyArray;
                }
            }
        }
    }

    // Read-only property describing how many elements are in the List.
    public int Count => _size;

    // Sets or Gets the element at the given index.
    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // Following trick can reduce the range check by one
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _items[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            _items[index] = value;
            _version++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCompatibleObject(object? value)
    {
        // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
        // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
        return value is T || value == null && default(T) == null;
    }

    // Adds the given object to the end of this list. The size of the list is
    // increased by one. If required, the capacity of the list is doubled
    // before adding the new element.
    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        _version++;
        T[] array = _items;
        int size = _size;
        if ((uint)size < (uint)array.Length)
        {
            _size = size + 1;
            array[size] = item;
        }
        else
        {
            AddWithResize(item);
        }
    }

    // Non-inline from List.Add to improve its code quality as uncommon path
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddWithResize(T item)
    {
        Debug.Assert(_size == _items.Length);
        int size = _size;
        Grow(size + 1);
        _size = size + 1;
        _items[size] = item;
    }

    // Adds the elements of the given collection to the end of this list. If
    // required, the capacity of the list is increased to twice the previous
    // capacity or the new size, whichever is larger.
    //
    public void AddRange(IEnumerable<T> collection) => InsertRange(_size, collection);

    // Searches a section of the list for a given element using a binary search
    // algorithm. Elements of the list are compared to the search value using
    // the given IComparer interface. If comparer is null, elements of
    // the list are compared to the search value using the IComparable
    // interface, which in that case must be implemented by all elements of the
    // list and the given search value. This method assumes that the given
    // section of the list is already sorted; if this is not the case, the
    // result will be incorrect.
    //
    // The method returns the index of the given value in the list. If the
    // list does not contain the given value, the method returns a negative
    // integer. The bitwise complement operator (~) can be applied to a
    // negative result to produce the index of the first element (if any) that
    // is larger than the given search value. This is also the index at which
    // the search value should be inserted into the list in order for the list
    // to remain sorted.
    //
    // The method uses the Array.BinarySearch method to perform the
    // search.
    //
    public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (_size - index < count)
        {
            throw new ArgumentException("Length must be greater than zero");
        }

        return Array.BinarySearch(_items, index, count, item, comparer);
    }

    public int BinarySearch(T item) => BinarySearch(0, Count, item, null);

    public int BinarySearch(T item, IComparer<T>? comparer) => BinarySearch(0, Count, item, comparer);

    // Clears the contents of List.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _version++;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            int size = _size;
            _size = 0;
            if (size > 0)
            {
                Array.Clear(_items, 0, size); // Clear the elements so that the gc can reclaim the references.
            }
        }
        else
        {
            _size = 0;
        }
    }

    // Contains returns true if the specified element is in the List.
    // It does a linear, O(n) search.  Equality is determined by calling
    // EqualityComparer<T>.Default.Equals().
    //
    public bool Contains(T item)
    {
        // PERF: IndexOf calls Array.IndexOf, which internally
        // calls EqualityComparer<T>.Default.IndexOf, which
        // is specialized for different types. This
        // boosts performance since instead of making a
        // virtual method call each iteration of the loop,
        // via EqualityComparer<T>.Default.Equals, we
        // only make one virtual call to EqualityComparer.IndexOf.

        return _size != 0 && IndexOf(item) != -1;
    }

    public PooledRefList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
    {
        if (converter == null)
        {
            throw new ArgumentNullException(nameof(converter));
        }

        PooledRefList<TOutput> list = new PooledRefList<TOutput>(_size);
        for (int i = 0; i < _size; i++)
        {
            list._items[i] = converter(_items[i]);
        }
        list._size = _size;

        return list;
    }

    // Copies this List into array, which must be of a
    // compatible array type.
    public void CopyTo(T[] array) => CopyTo(array, 0);

    // Copies a section of this list to the given array at the given index.
    //
    // The method uses the Array.Copy method to copy the elements.
    //
    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
        if (_size - index < count)
        {
            throw new ArgumentException("Length must be greater than zero");
        }

        // Delegate rest of error checking to Array.Copy.
        Array.Copy(_items, index, array, arrayIndex, count);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        // Delegate rest of error checking to Array.Copy.
        Array.Copy(_items, 0, array, arrayIndex, _size);
    }

    /// <summary>
    /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.
    /// If the current capacity of the list is less than specified <paramref name="capacity"/>,
    /// the capacity is increased by continuously twice current capacity until it is at least the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }
        if (_items.Length < capacity)
        {
            Grow(capacity);
            _version++;
        }

        return _items.Length;
    }

    /// <summary>
    /// Increase the capacity of this list to at least the specified <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    private void Grow(int capacity)
    {
        Debug.Assert(_items.Length < capacity);

        int newcapacity = _items.Length == 0 ? DefaultCapacity : 2 * _items.Length;

        // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint)newcapacity > MaxLength)
        {
            newcapacity = MaxLength;
        }

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < capacity)
        {
            newcapacity = capacity;
        }

        Capacity = newcapacity;
    }

    public bool Exists(Predicate<T> match) => FindIndex(match) != -1;

    public T? Find(Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        for (int i = 0; i < _size; i++)
        {
            if (match(_items[i]))
            {
                return _items[i];
            }
        }
        return default;
    }

    public PooledRefList<T> FindAll(Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        PooledRefList<T> list = new PooledRefList<T>();
        for (int i = 0; i < _size; i++)
        {
            if (match(_items[i]))
            {
                list.Add(_items[i]);
            }
        }
        return list;
    }

    public int FindIndex(Predicate<T> match) => FindIndex(0, _size, match);

    public int FindIndex(int startIndex, Predicate<T> match) => FindIndex(startIndex, _size - startIndex, match);

    public int FindIndex(int startIndex, int count, Predicate<T> match)
    {
        if ((uint)startIndex > (uint)_size)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex));
        }

        if (count < 0 || startIndex > _size - count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        int endIndex = startIndex + count;
        for (int i = startIndex; i < endIndex; i++)
        {
            if (match(_items[i]))
            {
                return i;
            }
        }
        return -1;
    }

    public T? FindLast(Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        for (int i = _size - 1; i >= 0; i--)
        {
            if (match(_items[i]))
            {
                return _items[i];
            }
        }
        return default;
    }

    public int FindLastIndex(Predicate<T> match) => FindLastIndex(_size - 1, _size, match);

    public int FindLastIndex(int startIndex, Predicate<T> match) => FindLastIndex(startIndex, startIndex + 1, match);

    public int FindLastIndex(int startIndex, int count, Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        if (_size == 0)
        {
            // Special case for 0 length List
            if (startIndex != -1)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
        }
        else
        {
            // Make sure we're not out of range
            if ((uint)startIndex >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
        }

        // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
        if (count < 0 || startIndex - count + 1 < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        int endIndex = startIndex - count;
        for (int i = startIndex; i > endIndex; i--)
        {
            if (match(_items[i]))
            {
                return i;
            }
        }
        return -1;
    }

    public void ForEach(Action<T> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        int version = _version;

        for (int i = 0; i < _size; i++)
        {
            if (version != _version)
            {
                break;
            }
            action(_items[i]);
        }

        if (version != _version)
        {
            throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
        }
    }

    // Returns an enumerator for this list with the given
    // permission for removal of elements. If modifications made to the list
    // while an enumeration is in progress, the MoveNext and
    // GetObject methods of the enumerator will throw an exception.
    //
    public Enumerator GetEnumerator() => new(this);

    public PooledRefList<T> GetRange(int index, int count)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (_size - index < count)
        {
            throw new ArgumentException("Length must be greater than zero");
        }

        PooledRefList<T> list = new PooledRefList<T>(count);
        Array.Copy(_items, index, list._items, 0, count);
        list._size = count;
        return list;
    }

    // Returns the index of the first occurrence of a given value in a range of
    // this list. The list is searched forwards from beginning to end.
    // The elements of the list are compared to the given value using the
    // Object.Equals method.
    //
    // This method uses the Array.IndexOf method to perform the
    // search.
    //
    public int IndexOf(T item) => Array.IndexOf(_items, item, 0, _size);

    // Returns the index of the first occurrence of a given value in a range of
    // this list. The list is searched forwards, starting at index
    // index and ending at count number of elements. The
    // elements of the list are compared to the given value using the
    // Object.Equals method.
    //
    // This method uses the Array.IndexOf method to perform the
    // search.
    //
    public int IndexOf(T item, int index)
    {
        if (index > _size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return Array.IndexOf(_items, item, index, _size - index);
    }

    // Returns the index of the first occurrence of a given value in a range of
    // this list. The list is searched forwards, starting at index
    // index and upto count number of elements. The
    // elements of the list are compared to the given value using the
    // Object.Equals method.
    //
    // This method uses the Array.IndexOf method to perform the
    // search.
    //
    public int IndexOf(T item, int index, int count)
    {
        if (index > _size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0 || index > _size - count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return Array.IndexOf(_items, item, index, count);
    }

    // Inserts an element into this list at a given index. The size of the list
    // is increased by one. If required, the capacity of the list is doubled
    // before inserting the new element.
    //
    public void Insert(int index, T item)
    {
        // Note that insertions at the end are legal.
        if ((uint)index > (uint)_size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        if (_size == _items.Length)
        {
            Grow(_size + 1);
        }

        if (index < _size)
        {
            Array.Copy(_items, index, _items, index + 1, _size - index);
        }
        _items[index] = item;
        _size++;
        _version++;
    }

    // Inserts the elements of the given collection at a given index. If
    // required, the capacity of the list is increased to twice the previous
    // capacity or the new size, whichever is larger.  Ranges may be added
    // to the end of the list by setting index to the List's size.
    //
    public void InsertRange(int index, IEnumerable<T> collection)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if ((uint)index > (uint)_size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (collection is ICollection<T> c)
        {
            int count = c.Count;
            if (count > 0)
            {
                if (_items.Length - _size < count)
                {
                    Grow(_size + count);
                }
                if (index < _size)
                {
                    Array.Copy(_items, index, _items, index + count, _size - index);
                }

                c.CopyTo(_items, index);
                _size += count;
            }
        }
        else
        {
            using IEnumerator<T> en = collection.GetEnumerator();
            while (en.MoveNext())
            {
                Insert(index++, en.Current);
            }
        }
        _version++;
    }

    // Returns the index of the last occurrence of a given value in a range of
    // this list. The list is searched backwards, starting at the end
    // and ending at the first element in the list. The elements of the list
    // are compared to the given value using the Object.Equals method.
    //
    // This method uses the Array.LastIndexOf method to perform the
    // search.
    //
    public int LastIndexOf(T item)
    {
        if (_size == 0)
        { // Special case for empty list
            return -1;
        }

        return LastIndexOf(item, _size - 1, _size);
    }

    // Returns the index of the last occurrence of a given value in a range of
    // this list. The list is searched backwards, starting at index
    // index and ending at the first element in the list. The
    // elements of the list are compared to the given value using the
    // Object.Equals method.
    //
    // This method uses the Array.LastIndexOf method to perform the
    // search.
    //
    public int LastIndexOf(T item, int index)
    {
        if (index >= _size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return LastIndexOf(item, index, index + 1);
    }

    // Returns the index of the last occurrence of a given value in a range of
    // this list. The list is searched backwards, starting at index
    // index and upto count elements. The elements of
    // the list are compared to the given value using the Object.Equals
    // method.
    //
    // This method uses the Array.LastIndexOf method to perform the
    // search.
    //
    public int LastIndexOf(T item, int index, int count)
    {
        if (Count != 0 && index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (Count != 0 && count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (_size == 0)
        { // Special case for empty list
            return -1;
        }

        if (index >= _size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count > index + 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return Array.LastIndexOf(_items, item, index, count);
    }

    // Removes the element at the given index. The size of the list is
    // decreased by one.
    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    // This method removes all items which matches the predicate.
    // The complexity is O(n).
    public int RemoveAll(Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        int freeIndex = 0; // the first free slot in items array

        // Find the first item which needs to be removed.
        while (freeIndex < _size && !match(_items[freeIndex]))
        {
            freeIndex++;
        }

        if (freeIndex >= _size)
        {
            return 0;
        }

        int current = freeIndex + 1;
        while (current < _size)
        {
            // Find the first item which needs to be kept.
            while (current < _size && match(_items[current]))
            {
                current++;
            }

            if (current < _size)
            {
                // copy item to the free slot.
                _items[freeIndex++] = _items[current++];
            }
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(_items, freeIndex, _size - freeIndex); // Clear the elements so that the gc can reclaim the references.
        }

        int result = _size - freeIndex;
        _size = freeIndex;
        _version++;
        return result;
    }

    // Removes the element at the given index. The size of the list is
    // decreased by one.
    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        _size--;
        if (index < _size)
        {
            Array.Copy(_items, index + 1, _items, index, _size - index);
        }
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _items[_size] = default!;
        }
        _version++;
    }

    // Removes a range of elements from this list.
    public void RemoveRange(int index, int count)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (_size - index < count)
        {
            throw new ArgumentException("Length must be greater than zero");
        }

        if (count > 0)
        {
            _size -= count;
            if (index < _size)
            {
                Array.Copy(_items, index + count, _items, index, _size - index);
            }

            _version++;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_items, _size, count);
            }
        }
    }

    // Reverses the elements in this list.
    public void Reverse() => Reverse(0, Count);

    // Reverses the elements in a range of this list. Following a call to this
    // method, an element in the range given by index and count
    // which was previously located at index i will now be located at
    // index index + (index + count - i - 1).
    //
    public void Reverse(int index, int count)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (_size - index < count)
        {
            throw new ArgumentException("Length must be greater than zero");
        }

        if (count > 1)
        {
            Array.Reverse(_items, index, count);
        }
        _version++;
    }

    // Sorts the elements in this list.  Uses the default comparer and
    // Array.Sort.
    public void Sort() => Sort(0, Count, null);

    // Sorts the elements in this list.  Uses Array.Sort with the
    // provided comparer.
    public void Sort(IComparer<T>? comparer) => Sort(0, Count, comparer);

    // Sorts the elements in a section of this list. The sort compares the
    // elements to each other using the given IComparer interface. If
    // comparer is null, the elements are compared to each other using
    // the IComparable interface, which in that case must be implemented by all
    // elements of the list.
    //
    // This method uses the Array.Sort method to sort the elements.
    //
    public void Sort(int index, int count, IComparer<T>? comparer)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (_size - index < count)
        {
            throw new ArgumentException("Length must be greater than zero");
        }

        if (count > 1)
        {
            Array.Sort(_items, index, count, comparer);
        }
        _version++;
    }

    public void Sort(Comparison<T> comparison)
    {
        if (comparison == null)
        {
            throw new ArgumentNullException(nameof(comparison));
        }

        if (_size > 1)
        {
            Array.Sort(_items, comparison);
        }
        _version++;
    }

    // ToArray returns an array containing the contents of the List.
    // This requires copying the List, which is an O(n) operation.
    public T[] ToArray()
    {
        if (_size == 0)
        {
            return s_emptyArray;
        }

        T[] array = new T[_size];
        Array.Copy(_items, array, _size);
        return array;
    }

    public T[] ToPooledArray()
    {
        if (_size == 0)
        {
            return s_emptyArray;
        }

        T[] array = ArrayPool.Rent(_size);
        Array.Copy(_items, array, _size);
        return array;
    }

    // Sets the capacity of this list to the size of the list. This method can
    // be used to minimize a list's memory overhead once it is known that no
    // new elements will be added to the list. To completely clear a list and
    // release all memory referenced by the list, execute the following
    // statements:
    //
    // list.Clear();
    // list.TrimExcess();
    //
    public void TrimExcess()
    {
        int threshold = (int)(_items.Length * 0.9);
        if (_size < threshold)
        {
            Capacity = _size;
        }
    }

    public bool TrueForAll(Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        for (int i = 0; i < _size; i++)
        {
            if (!match(_items[i]))
            {
                return false;
            }
        }
        return true;
    }

    public ref struct Enumerator
    {
        private readonly PooledRefList<T> _list;
        private int _index;
        private readonly int _version;
        private T? _current;

        internal Enumerator(PooledRefList<T> list)
        {
            _list = list;
            _index = 0;
            _version = list._version;
            _current = default;
        }

        public void Dispose()
        {
            _index = -2;
            _current = default;
        }

        public bool MoveNext()
        {
            PooledRefList<T> localList = _list;

            if (_version == localList._version && (uint)_index < (uint)localList._size)
            {
                _current = localList._items[_index];
                _index++;
                return true;
            }
            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            if (_version != _list._version)
            {
                throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
            }

            _index = _list._size + 1;
            _current = default;
            return false;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_index == 0 || _index == _list._size + 1)
                {
                    ThrowEnumerationNotStartedOrEnded();
                }
                return Current;
            }
        }

        private void ThrowEnumerationNotStartedOrEnded()
        {
            Debug.Assert(_index is -1 or -2);
            throw new InvalidOperationException(_index == -1 ? CollectionThrowStrings.InvalidOperation_EnumNotStarted : CollectionThrowStrings.InvalidOperation_EnumEnded);
        }

        public void Reset()
        {
            if (_version != _list._version)
            {
                throw new InvalidOperationException(CollectionThrowStrings.InvalidOperation_EnumFailedVersion);
            }

            _index = -1;
            _current = default;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        var array = _items;

        if (array.Length > 0)
        {
            Clear();
            ArrayPool.Return(_items);
        }

        this = default;
    }
}
