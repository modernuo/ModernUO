/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableFieldAttributeAttribute.cs                          *
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
    /// Hints to the source generator that this field will need this attribute on the generated property
    /// [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    /// -or-
    /// [SerializableFieldAttr(typeof(CommandPropertyAttribute), AccessLevel.GameMaster)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class SerializableFieldAttrAttribute : Attribute
    {
        public string AttributeString { get; }
        public Type AttributeType { get; }
        public object[] Arguments { get; }

        public SerializableFieldAttrAttribute(string attrString) => AttributeString = attrString;

        public SerializableFieldAttrAttribute(Type type, params object[] args)
        {
            if (typeof(Attribute).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Argument {nameof(type)} must be an attribute.");
            }

            AttributeType = type;
            Arguments = args;
        }
    }
}
