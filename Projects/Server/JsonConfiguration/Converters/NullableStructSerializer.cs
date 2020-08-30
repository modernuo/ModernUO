namespace System.Text.Json.Serialization
{
    public class NullableStructSerializer<TStruct> : JsonConverter<TStruct?> where TStruct : struct
    {
        public override TStruct? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null
                ? default
                : JsonSerializer.Deserialize<TStruct>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, TStruct? value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
