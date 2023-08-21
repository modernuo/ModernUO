using System;
using Xunit;

using System.Buffers;
using Server.Json;
using System.Text.Json;
using System.IO;
using Microsoft.Toolkit.HighPerformance;
using System.Text;
using Server.Engines.Plants;

namespace Server.Tests;

public class TypeConverterTests
{
    [Fact]
    public void TestReadAfterWrite()
    {
        // Given
        Type itemType = typeof(Item);
        Core.Assembly = itemType.Assembly;
        ServerConfiguration.Load(true);
        string assemblyLocation = itemType.Assembly.Location;
        AssemblyHandler.LoadAssemblies(new string[] { assemblyLocation });

        TypeConverter converter = new TypeConverter();
        var bufferWriter = new ArrayBufferWriter<byte>();
        Utf8JsonWriter jsonWriter = new Utf8JsonWriter(bufferWriter);
        converter.Write(jsonWriter, itemType, new JsonSerializerOptions());

        // When
        jsonWriter.Flush();
        var jsonData = bufferWriter.WrittenSpan;
        var writtenString = Encoding.UTF8.GetString(jsonData.ToArray());
        Utf8JsonReader jsonReader = new Utf8JsonReader(jsonData);
        jsonReader.Read();

        // Then
        Type readType = converter.Read(ref jsonReader, null, new JsonSerializerOptions());
        Assert.Equal(typeof(Item), readType);
    }
}
