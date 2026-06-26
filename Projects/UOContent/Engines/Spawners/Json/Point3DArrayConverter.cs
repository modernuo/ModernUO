/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Point3DArrayConverter.cs                                        *
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
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Json;

namespace Server.Engines.Spawners;

/// <summary>
/// Writes a <see cref="Point3D"/> as the compact array form <c>[x, y, z]</c> used by the spawn
/// files (the default <see cref="Point3DConverter"/> writes an object). Reading delegates to the
/// default converter, so array/object/string inputs all still load.
/// </summary>
public sealed class Point3DArrayConverter : JsonConverter<Point3D>
{
    private static readonly Point3DConverter _inner = new();

    public override Point3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        _inner.Read(ref reader, typeToConvert, options);

    public override void Write(Utf8JsonWriter writer, Point3D value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}
