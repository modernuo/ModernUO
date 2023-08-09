/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: JsonConfig.cs                                                   *
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
using System.Buffers;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Server.Text;

namespace Server.Json;

public static class JsonConfig
{
    public static readonly JsonSerializerOptions DefaultOptions = GetOptions();

    public static JsonSerializerOptions GetOptions(params JsonConverterFactory[] converters)
    {
        // In the future this should be optimized by cloning DefaultOptions
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        options.Converters.Add(new ClientVersionConverterFactory());
        options.Converters.Add(new GuidConverterFactory());
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new MapConverterFactory());
        options.Converters.Add(new Point3DConverterFactory());
        options.Converters.Add(new Rectangle3DConverterFactory());
        options.Converters.Add(new TimeSpanConverterFactory());
        options.Converters.Add(new IPEndPointConverterFactory());
        options.Converters.Add(new TypeConverterFactory());
        options.Converters.Add(new WorldLocationConverterFactory());

        for (var i = 0; i < converters.Length; i++)
        {
            options.Converters.Add(converters[i]);
        }

        return options;
    }

    public static T Deserialize<T>(string filePath, JsonSerializerOptions options = null)
    {
        if (!File.Exists(filePath))
        {
            return default;
        }

        var text = File.ReadAllText(filePath, TextEncoding.UTF8);
        return JsonSerializer.Deserialize<T>(text, options ?? DefaultOptions);
    }

    public static string Serialize(object value, JsonSerializerOptions options = null) =>
        JsonSerializer.Serialize(value, options ?? DefaultOptions);

    public static void Serialize(string filePath, object value, JsonSerializerOptions options = null)
    {
        var contents = Serialize(value, options);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        File.WriteAllText(filePath, contents);
    }

    public static T ToObject<T>(this ref Utf8JsonReader reader, JsonSerializerOptions options = null) =>
        JsonSerializer.Deserialize<T>(ref reader, options);

    public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            element.WriteTo(writer);
        }

        return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options);
    }

    public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions options = null)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return document.RootElement.ToObject<T>(options);
    }
}
