/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: JsonConfig.cs - Created: 2020/05/02 - Updated: 2020/05/02       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Buffers;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Json
{
  public static class JsonConfig
  {
    public static readonly JsonSerializerOptions DefaultOptions = GetOptions();

    public static JsonSerializerOptions GetOptions(params JsonConverterFactory[] converters)
    {
      // In the future this should be optimized by cloning DefaultOptions
      var options = new JsonSerializerOptions
      {
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true,
        AllowTrailingCommas = true,
        IgnoreNullValues = true
      };

      options.Converters.Add(new MapConverterFactory());
      options.Converters.Add(new Point3DConverterFactory());
      options.Converters.Add(new Rectangle3DConverterFactory());
      options.Converters.Add(new TimeSpanConverterFactory());
      options.Converters.Add(new IPEndPointConverterFactory());

      for (int i = 0; i < converters.Length; i++) options.Converters.Add(converters[i]);

      return options;
    }

    public static T Deserialize<T>(string filePath, JsonSerializerOptions options = null)
    {
      if (!File.Exists(filePath)) return default;
      string text = File.ReadAllText(filePath, Utility.UTF8);
      return JsonSerializer.Deserialize<T>(text, options ?? DefaultOptions);
    }

    public static void Serialize(string filePath, object value, JsonSerializerOptions options = null)
    {
      if (File.Exists(filePath)) File.Delete(filePath);

      File.WriteAllText(filePath, JsonSerializer.Serialize(value, options ?? DefaultOptions));
    }

    public static T ToObject<T>(this ref Utf8JsonReader reader, JsonSerializerOptions options = null) =>
      JsonSerializer.Deserialize<T>(ref reader, options);

    public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
    {
      var bufferWriter = new ArrayBufferWriter<byte>();
      using (var writer = new Utf8JsonWriter(bufferWriter))
        element.WriteTo(writer);
      return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options);
    }

    public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions options = null)
    {
      if (document == null)
        throw new ArgumentNullException(nameof(document));
      return document.RootElement.ToObject<T>(options);
    }
  }
}
