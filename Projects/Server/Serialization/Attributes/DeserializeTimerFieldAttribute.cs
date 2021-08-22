/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DeserializeTimerFieldAttribute.cs                               *
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
    /// Hints to the source generator that the specified serializable field, which must be a timer,
    /// can be deserialized by this method. The method signature should look like this:
    ///
    /// [DeserializeTimerField(0)]
    /// private void DeserializeTimer(TimeSpan delay)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DeserializeTimerFieldAttribute : Attribute
    {
        public int Order { get; }

        public DeserializeTimerFieldAttribute(int order) => Order = order;
    }
}
