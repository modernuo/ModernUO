using System;
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

      var data = new int[3];
      var count = 0;

      while (true)
      {
        reader.Read();
        if (reader.TokenType == JsonTokenType.EndArray)
          break;

        if (reader.TokenType == JsonTokenType.Number)
        {
          if (count < 3)
            data[count] = reader.GetInt32();

          count++;
        }
      }

      if (count < 2 || count > 3)
        throw new JsonException("Point3d must be an array of x, y, z");

      return new Point3D(data[0], data[1], data[2]);
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
