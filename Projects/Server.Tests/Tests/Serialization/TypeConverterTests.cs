using System;
using System.Buffers;
using System.Text.Json;

using Server.Json;

using Xunit;

namespace Server.Tests;

public class TypeConverterTests : IClassFixture<ServerFixture>
{
    [Fact]
    public void TestReadAfterWrite()
    {
        // Arrange
        Type itemType = typeof(Item);

        JsonSerializerOptions options = new JsonSerializerOptions();

        TypeConverter converter = new TypeConverter();
        var bufferWriter = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        converter.Write(jsonWriter, itemType, options);

        // Act
        jsonWriter.Flush();
        var jsonData = bufferWriter.WrittenSpan;

        Utf8JsonReader jsonReader = new Utf8JsonReader(jsonData);
        jsonReader.Read();

        // Assert
        Type readType = converter.Read(ref jsonReader, typeof(Type), options);
        Assert.Equal(typeof(Item), readType);
    }
}
