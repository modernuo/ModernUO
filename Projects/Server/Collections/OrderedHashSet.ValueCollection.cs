/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: OrderedHashSet.ValueCollection.cs                               *
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

namespace Server.Collections
{
    public partial class OrderedHashSet<TValue>
    {
        [DebuggerDisplay("Count = {Count}")]
        public sealed class ValueCollection : IList<TValue>, IReadOnlyList<TValue>
        {
            private readonly OrderedHashSet<TValue> _orderedHashSet;

            private const string NotSupported_ValueCollectionSet =
                "Mutating a key collection derived from a hash set is not allowed.";

            public int Count => _orderedHashSet.Count;

            public TValue this[int index] => ((IList<TValue>)_orderedHashSet)[index];

            TValue IList<TValue>.this[int index]
            {
                get => this[index];
                set => throw new NotSupportedException(NotSupported_ValueCollectionSet);
            }

            bool ICollection<TValue>.IsReadOnly => true;

            internal ValueCollection(OrderedHashSet<TValue> OrderedHashSet) =>
                _orderedHashSet = OrderedHashSet;

            public Enumerator GetEnumerator() => new Enumerator(_orderedHashSet);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            int IList<TValue>.IndexOf(TValue item) => _orderedHashSet.IndexOf(item);

            void IList<TValue>.Insert(int index, TValue item) => throw new NotSupportedException(NotSupported_ValueCollectionSet);

            void IList<TValue>.RemoveAt(int index) => throw new NotSupportedException(NotSupported_ValueCollectionSet);

            void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException(NotSupported_ValueCollectionSet);

            void ICollection<TValue>.Clear() => throw new NotSupportedException(NotSupported_ValueCollectionSet);

            bool ICollection<TValue>.Contains(TValue item) => _orderedHashSet.Contains(item);

            void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex)
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

                Entry[] entries = _orderedHashSet._entries;
                for (int i = 0; i < count; ++i)
                {
                    array[i + arrayIndex] = entries[i].Value;
                }
            }

            bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException(NotSupported_ValueCollectionSet);

            public struct Enumerator : IEnumerator<TValue>
            {
                private readonly OrderedHashSet<TValue> _orderedHashSet;
                private readonly int _version;
                private int _index;
                private TValue _current;

                public TValue Current => _current;

                object IEnumerator.Current => _current;

                internal Enumerator(OrderedHashSet<TValue> OrderedHashSet)
                {
                    _orderedHashSet = OrderedHashSet;
                    _version = OrderedHashSet._version;
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
                        _current = _orderedHashSet._entries[_index].Value;
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
}
