/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: ArraySet.cs - Created: 2019/10/04 - Updated: 2019/12/30         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Collections
{
    public class ArraySet<T> : IList<T>
    {
        private readonly List<T> m_List = new List<T>();

        public T this[int index]
        {
            get => m_List[index];
            set => m_List[index] = value;
        }

        public int Count => m_List.Count;

        public bool IsReadOnly => false;

        public void Clear() => m_List.Clear();

        public bool Contains(T item) => m_List.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => m_List.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => m_List.GetEnumerator();

        public int IndexOf(T item) => m_List.IndexOf(item);

        public void Insert(int index, T item) => throw new NotImplementedException();

        public bool Remove(T item) => throw new NotImplementedException();

        public void RemoveAt(int index) => throw new NotImplementedException();

        void ICollection<T>.Add(T item) => m_List.Add(item);

        IEnumerator IEnumerable.GetEnumerator() => m_List.GetEnumerator();

        public int Add(T item)
        {
            var indexOf = m_List.IndexOf(item);

            if (indexOf >= 0) return indexOf;

            m_List.Add(item);
            return m_List.Count - 1;
        }

        public void CopyTo(T[] array) => m_List.CopyTo(array);

        public void CopyTo(int index, T[] array, int arrayIndex, int count) =>
            m_List.CopyTo(index, array, arrayIndex, count);
    }
}
