namespace System.Text.Json.Serialization
{
    public class NullableStructSerializerFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType || typeToConvert.GetGenericTypeDefinition() != typeof(Nullable<>))
                return false;

            var structType = typeToConvert.GenericTypeArguments[0];
            return !structType.IsPrimitive && structType.Namespace?.StartsWith(nameof(System)) != true && !structType.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(NullableStructSerializer<>).MakeGenericType(typeToConvert.GenericTypeArguments[0])
            );
    }
}
