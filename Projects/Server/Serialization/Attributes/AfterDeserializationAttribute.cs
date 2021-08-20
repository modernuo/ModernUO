/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AfterDeserializationAttribute.cs                                *
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
    /// Hints to the source generator that this method should be executed after deserializing the object.
    /// Method must have no parameters and return void.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AfterDeserializationAttribute : Attribute
    {
        /// <summary>
        /// Indicates whether the source generator should execute the method this is attached to immediately, or when
        /// this is set to false, execute it using a Timer Delay.
        ///
        /// Note: Use false when the after deserialization involves deleting objects. This is to prevent corrupted
        /// deserialization by removing an object before it has finished deserializing.
        /// </summary>
        public bool Synchronous { get; set; }

        public AfterDeserializationAttribute(bool synchronous = true) => Synchronous = true;
    }
}
