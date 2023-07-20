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
        public void Enable_all_maps()
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
        public void Disable_individual_map()
        {
            MapSelection mapSelection = new();
            mapSelection.EnableAll();

            // When
            mapSelection.Disable(MapSelectionFlags.Trammel);

            // Then
            Assert.False(mapSelection.Includes(MapSelectionFlags.Trammel));
        }

        [Fact]
        public void Enable_individual_map()
        {
            MapSelection mapSelection = new();

            // When
            mapSelection.Enable(MapSelectionFlags.Trammel);

            // Then
            Assert.True(mapSelection.Includes(MapSelectionFlags.Trammel));
        }

        [Fact]
        public void Format_an_empty_map_selection()
        {
            MapSelection mapSelection = new();

            // When, Then
            Assert.Equal("None", mapSelection.ToCommaDelimitedString());
        }

        [Fact]
        public void Format_felucca_only_map_selection()
        {
            MapSelection mapSelection = new();
            mapSelection.Enable(MapSelectionFlags.Felucca);

            // When, Then
            Assert.Equal("Felucca", mapSelection.ToCommaDelimitedString());
        }

        [Fact]
        public void Format_felucca_and_trammel_map_selection()
        {
            MapSelection mapSelection = new();
            mapSelection.Enable(MapSelectionFlags.Felucca);
            mapSelection.Enable(MapSelectionFlags.Trammel);

            // When, Then
            Assert.Equal("Felucca, Trammel", mapSelection.ToCommaDelimitedString());
        }

        [Fact]
        public void Can_evaluate_enabled_maps_by_string()
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

        [Fact]
        public void Flags_accessible_as_a_list()
        {
            MapSelection mapSelection = new();
            mapSelection.Enable(MapSelectionFlags.Felucca);
            mapSelection.Enable(MapSelectionFlags.Ilshenar);
            mapSelection.Enable(MapSelectionFlags.Tokuno);

            // When
            List<string> actual = mapSelection.ToList();

            // Then
            var expected = new List<string>
            {
                "Felucca", "Ilshenar", "Tokuno"
            };

            Assert.Equal(expected, actual);
        }

        public class TestMapConfig
        {
            [JsonConverter(typeof(FlagsConverter<MapSelectionFlags>))]
            public MapSelectionFlags TestMapFlags { get; set; }
        }

        [Fact]
        public void Serialize_and_deserialize_map_selection_flags()
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
