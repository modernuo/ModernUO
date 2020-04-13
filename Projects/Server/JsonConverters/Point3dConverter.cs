using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Json
{
  public class Point3dConverter : JsonConverter<Point3D>
  {
    public override Point3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartArray)
        throw new JsonException("Point3d must be an array of x, y, z");

      var data = new List<int>();

      while (true)
      {
        reader.Read();
        if (reader.TokenType == JsonTokenType.EndArray)
          break;

        if (reader.TokenType == JsonTokenType.Number)
          data.Add(reader.GetInt32());
      }

      if (data.Count < 2 || data.Count > 3)
        throw new JsonException("Point3d must be an array of x, y, z");

      return new Point3D(data[0], data[1], data.Count == 3 ? data[2] : 0);
    }
    public override void Write(Utf8JsonWriter writer, Point3D value, JsonSerializerOptions options)
    {
      writer.WriteStartArray();
      writer.WriteNumberValue(value.X);
      writer.WriteNumberValue(value.Y);
      writer.WriteNumberValue(value.Z);
      writer.WriteEndArray();
    }
  }
}
