/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: JsonDiscoverableTypeAttribute.cs                                *
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

namespace Server.Json;

/// <summary>
/// Marks a concrete class as a discoverable polymorphic JSON derived type. A consumer
/// (e.g. <c>SpawnerJsonSerializer</c>) scans assemblies for marked subclasses of a chosen
/// base and registers them for System.Text.Json polymorphism. Optionally overrides the
/// <c>$type</c> discriminator value (defaults to the type's <see cref="System.Type.Name"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class JsonDiscoverableTypeAttribute : Attribute
{
    public JsonDiscoverableTypeAttribute(string discriminator = null) => Discriminator = discriminator;

    public string Discriminator { get; }
}
