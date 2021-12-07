/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializablePropertyComparer.cs                                 *
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

namespace SerializableMigration;

public class SerializablePropertyComparer : IComparer<SerializableProperty>
{
    public int Compare(SerializableProperty x, SerializableProperty y)
    {
        if (Equals(x, y))
        {
            return 0;
        }

        if (Equals(null, y))
        {
            return 1;
        }

        if (Equals(null, x))
        {
            return -1;
        }

        return x.Order.CompareTo(y.Order);
    }
}