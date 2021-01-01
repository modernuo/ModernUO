/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: IndexList.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;

namespace Server.Collections
{
    public class IndexList<T>
    {
        private int _index;
        private Dictionary<T, int> _table;

        public IndexList() => _table = new Dictionary<T, int>();

        public IndexList(int initialCapacity) => _table = new Dictionary<T, int>(initialCapacity);

        public int Add(T item)
        {
            if (_table.TryGetValue(item, out var index))
            {
                return index;
            }

            _table.Add(item, _index);
            return _index++;
        }

        public bool Remove(T item) => _table.Remove(item);

        public int Count => _table.Count;

        public IEnumerator<T> GetEnumerator() => _table.Keys.GetEnumerator();
    }
}
