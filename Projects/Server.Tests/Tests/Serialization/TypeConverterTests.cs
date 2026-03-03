using System;
using System.Buffers;
using System.Text.Json;

using Server.Json;

using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class TypeConverterTests
{
    [Fact]
    public void TestReadAfterWrite()
    {
        // Arrange
        var itemType = typeof(Item);

        var options = new JsonSerializerOptions();

        var converter = new TypeConverter();
        var bufferWriter = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(bufferWriter);
        converter.Write(jsonWriter, itemType, options);

        // Act
        jsonWriter.Flush();
        var jsonData = bufferWriter.WrittenSpan;

        var jsonReader = new Utf8JsonReader(jsonData);
        jsonReader.Read();

        // Assert
        var readType = converter.Read(ref jsonReader, typeof(Type), options);
        Assert.Equal(typeof(Item), readType);
    }
}
