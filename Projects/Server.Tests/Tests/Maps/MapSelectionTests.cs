using Xunit;
using Server.Maps;
using Server.Json;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Server.Tests.Tests.Maps
{
    public class MapSelectionTests
    {
        [Theory]
        [InlineData(MapSelectionFlags.Felucca, "Felucca")]
        [InlineData(MapSelectionFlags.Felucca | MapSelectionFlags.Trammel, "Felucca, Trammel")]
        public void TestCommaSeparatedList(MapSelectionFlags flags, string expected)
        {
            Assert.Equal(expected, flags.ToCommaDelimitedString());
        }

        [Fact]
        public void TestFormatFeluccaOnlyMapSelection()
        {
            const MapSelectionFlags flags = MapSelectionFlags.Felucca;

            // When, Then
            Assert.Equal("Felucca", flags.ToCommaDelimitedString());
        }

        public class TestMapConfig
        {
            [JsonConverter(typeof(FlagsConverter<MapSelectionFlags>))]
            public MapSelectionFlags TestMapFlags { get; set; }
        }

        [Fact]
        public void TestSerializeAndDeserializeMapSelectionFlags()
        {
            TestMapConfig mapConfig = new()
            {
                TestMapFlags = MapSelectionFlags.Felucca | MapSelectionFlags.Ilshenar,
            };

            // When
            string serialized = JsonConfig.Serialize(mapConfig);
            TestMapConfig deserialized = JsonSerializer.Deserialize<TestMapConfig>(serialized, JsonConfig.GetOptions());

            // Then
            Assert.Equal(mapConfig.TestMapFlags, deserialized.TestMapFlags);
        }
    }
}
