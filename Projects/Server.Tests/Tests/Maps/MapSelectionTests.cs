using Xunit;
using Server.Maps;
using System.Collections.Generic;
using Server.Json;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Server.Tests.Tests.Maps
{
    public class MapSelectionTests
    {
        [Fact]
        public void No_maps_enabled_on_construction()
        {
            // When
            MapSelection mapSelection = new();

            // Then
            Assert.False(mapSelection.Includes(MapSelectionFlags.Felucca));
            Assert.False(mapSelection.Includes(MapSelectionFlags.Trammel));
            Assert.False(mapSelection.Includes(MapSelectionFlags.Ilshenar));
            Assert.False(mapSelection.Includes(MapSelectionFlags.Malas));
            Assert.False(mapSelection.Includes(MapSelectionFlags.Tokuno));
            Assert.False(mapSelection.Includes(MapSelectionFlags.TerMur));
        }

        [Fact]
        public void TestEnableAllMaps()
        {
            MapSelection mapSelection = new();

            // When
            mapSelection.EnableAll();

            // Then
            Assert.True(mapSelection.Includes(MapSelectionFlags.Felucca));
            Assert.True(mapSelection.Includes(MapSelectionFlags.Trammel));
            Assert.True(mapSelection.Includes(MapSelectionFlags.Ilshenar));
            Assert.True(mapSelection.Includes(MapSelectionFlags.Malas));
            Assert.True(mapSelection.Includes(MapSelectionFlags.Tokuno));
            Assert.True(mapSelection.Includes(MapSelectionFlags.TerMur));
        }

        [Fact]
        public void TestDisableIndividualMap()
        {
            MapSelection mapSelection = new();
            mapSelection.EnableAll();

            // When
            mapSelection.Disable(MapSelectionFlags.Trammel);

            // Then
            Assert.False(mapSelection.Includes(MapSelectionFlags.Trammel));
        }

        [Fact]
        public void TestEnableIndividualMap()
        {
            MapSelection mapSelection = new();

            // When
            mapSelection.Enable(MapSelectionFlags.Trammel);

            // Then
            Assert.True(mapSelection.Includes(MapSelectionFlags.Trammel));
        }

        [Fact]
        public void TestFormatAnEmptyMapSelection()
        {
            MapSelection mapSelection = new();

            // When, Then
            Assert.Equal("None", mapSelection.ToCommaDelimitedString());
        }

        [Fact]
        public void TestFormatFeluccaOnlyMapSelection()
        {
            MapSelection mapSelection = new();
            mapSelection.Enable(MapSelectionFlags.Felucca);

            // When, Then
            Assert.Equal("Felucca", mapSelection.ToCommaDelimitedString());
        }

        [Fact]
        public void TestFormatFeluccaAndTrammelMapSelection()
        {
            MapSelection mapSelection = new();
            mapSelection.Enable(MapSelectionFlags.Felucca);
            mapSelection.Enable(MapSelectionFlags.Trammel);

            // When, Then
            Assert.Equal("Felucca, Trammel", mapSelection.ToCommaDelimitedString());
        }

        [Fact]
        public void TestCanEvaluateEnabledMapsByString()
        {
            MapSelection mapSelection = new();
            mapSelection.Enable(MapSelectionFlags.Felucca);

            // When
            bool feluccaIsEnabled = mapSelection.Includes("Felucca");
            bool trammelIsEnabled = mapSelection.Includes("Trammel");

            // Then
            Assert.True(feluccaIsEnabled);
            Assert.False(trammelIsEnabled);
        }

        public class TestMapConfig
        {
            [JsonConverter(typeof(FlagsConverter<MapSelectionFlags>))]
            public MapSelectionFlags TestMapFlags { get; set; }
        }

        [Fact]
        public void TestSerializeAndDeserializeMapSelectionFlags()
        {
            MapSelection mapSelection = new();
            mapSelection.Enable(MapSelectionFlags.Felucca);
            mapSelection.Enable(MapSelectionFlags.Trammel);

            TestMapConfig mapConfig = new();
            mapConfig.TestMapFlags = mapSelection.Flags;

            // When
            string serialized = JsonConfig.Serialize(mapConfig);
            TestMapConfig deserialized = JsonSerializer.Deserialize<TestMapConfig>(serialized, JsonConfig.GetOptions());

            // Then
            Assert.Equal(mapConfig.TestMapFlags, deserialized.TestMapFlags);
        }
    }
}
