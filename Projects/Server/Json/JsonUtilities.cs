/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: JsonPropertySorter.cs                                           *
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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Server.Json;

public static class JsonUtilities
{
    public static string SortByPropertyName(string jsonStr)
    {
        using JsonDocument doc = JsonDocument.Parse(jsonStr);
        return SortByPropertyName(doc.RootElement);
    }

    public static string SortByPropertyName(JsonElement je)
    {
        // TODO: Better way to do this than a stream?
        using var ms = new MemoryStream();
        JsonWriterOptions opts = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using (var writer = new Utf8JsonWriter(ms, opts))
        {
            WriteJsonElementSorted(je, writer);
        }

        ms.TryGetBuffer(out var buffer);
        return Encoding.UTF8.GetString(buffer);
    }

    private static void WriteJsonElementSorted(JsonElement je, Utf8JsonWriter writer)
    {
        switch(je.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();

                // TODO: This is slow, can make it faster?
                foreach (JsonProperty x in je.EnumerateObject().OrderBy(prop => prop.Name))
                {
                    writer.WritePropertyName(x.Name);
                    WriteJsonElementSorted(x.Value, writer);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach(JsonElement x in je.EnumerateArray())
                {
                    WriteJsonElementSorted(x, writer);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.Number:
                writer.WriteNumberValue(je.GetDouble());
                break;
            case JsonValueKind.String:
                // Escape the string
                writer.WriteStringValue(je.GetString());
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Undefined: // Don't write anything
                break;
            default:
                throw new NotImplementedException($"Kind: {je.ValueKind}");

        }
    }
}
