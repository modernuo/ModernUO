using System;
using System.Buffers;
using System.Text.Json;
using System.Text;
using System.Diagnostics;

using Server.Json;

using Xunit;

namespace Server.Tests;

public class TypeConverterTests
{
    bool AssemblyAlreadyLoaded(string assemblyLocation)
    {
        // If Assemblies is null or doesn't contain the assembly we want to load...
        if (AssemblyHandler.Assemblies == null)
        {
            return false;
        }

        foreach (var assembly in AssemblyHandler.Assemblies)
        {
            if (assembly.Location == assemblyLocation)
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void TestReadAfterWrite()
    {
        // Given
        Type itemType = typeof(Item);
        string assemblyLocation = itemType.Assembly.Location;
        JsonSerializerOptions options = new JsonSerializerOptions();

        if (!AssemblyAlreadyLoaded(assemblyLocation))
        {
            AssemblyHandler.LoadAssemblies(new string[] { assemblyLocation });
        }   

        TypeConverter converter = new TypeConverter();
        var bufferWriter = new ArrayBufferWriter<byte>();
        Utf8JsonWriter jsonWriter = new Utf8JsonWriter(bufferWriter);
        converter.Write(jsonWriter, itemType, options);

        // When
        jsonWriter.Flush();
        var jsonData = bufferWriter.WrittenSpan;
        var writtenString = Encoding.UTF8.GetString(jsonData.ToArray());
        Utf8JsonReader jsonReader = new Utf8JsonReader(jsonData);
        jsonReader.Read();

        // Then
        Type readType = converter.Read(ref jsonReader, null, options);
        Assert.Equal(typeof(Item), readType);
    }
}
