/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: SerializablePropertyAttribute.cs                                *
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

namespace Server
{
    /// <summary>
    /// Marks a property as serializable. Requires a call to ISerializable.MarkDirty()
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SerializablePropertyAttribute : Attribute
    {
        public int Order { get; }

        public SerializablePropertyAttribute(int order) => Order = order;
    }
}
