/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableFieldDefaultAttribute.cs                            *
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
    /// Hints to the source generator that the field with the same order should use this default value
    /// while deserializing. The default is used when the save flag indicates that we don't need to serialize the value
    /// because this default can be used instead.
    ///
    /// Note: This is only used for the current version, not previous versions. Previous versions will always use a default
    /// value for that type if it is not deserialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SerializableFieldDefaultAttribute : Attribute
    {
        public int Order { get; }

        public SerializableFieldDefaultAttribute(int order) => Order = order;
    }
}
