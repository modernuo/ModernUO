using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server.Json
{
  public class MapConverter : JsonConverter<Map>
  {
    public override Map Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
      => Map.Parse(reader.GetString());

    public override void Write(Utf8JsonWriter writer, Map value, JsonSerializerOptions options)
      => writer.WriteStringValue(value.Name);
  }
}
