using Xunit;
using Server.Maps;

namespace Server.Tests.Tests.Maps
{
    public class MapSelectionTests
    {
        [Fact]
        public void No_maps_enabled_on_construction()
        {
            // When
            MapSelection mapSelection = new MapSelection();

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
            MapSelection mapSelection = new MapSelection();

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
            MapSelection mapSelection = new MapSelection();
            mapSelection.EnableAll();

            // When
            mapSelection.Disable(MapSelectionFlags.Trammel);

            // Then
            Assert.False(mapSelection.Includes(MapSelectionFlags.Trammel));
        }

        [Fact]
        public void Enable_individual_map()
        {
            MapSelection mapSelection = new MapSelection();

            // When
            mapSelection.Enable(MapSelectionFlags.Trammel);

            // Then
            Assert.True(mapSelection.Includes(MapSelectionFlags.Trammel));
        }

        [Fact]
        public void Format_an_empty_map_selection()
        {
            MapSelection mapSelection = new MapSelection();

            // When, Then
            Assert.Equal("None", mapSelection.ToCommaDelimitedString());
        }


        [Fact]
        public void Format_felucca_only_map_selection()
        {
            MapSelection mapSelection = new MapSelection();
            mapSelection.Enable(MapSelectionFlags.Felucca);

            // When, Then
            Assert.Equal("Felucca", mapSelection.ToCommaDelimitedString());
        }

        [Fact]
        public void Format_felucca_and_trammel_map_selection()
        {
            MapSelection mapSelection = new MapSelection();
            mapSelection.Enable(MapSelectionFlags.Felucca);
            mapSelection.Enable(MapSelectionFlags.Trammel);

            // When, Then
            Assert.Equal("Felucca, Trammel", mapSelection.ToCommaDelimitedString());
        }

        [Fact]
        public void Enable_all_maps_in_a_particular_expansion()
        {
            MapSelection mapSelection = new MapSelection();
            Expansion expansion = Expansion.SE;

            // When
            mapSelection.EnableAllInExpansion(expansion);

            // Then
            Assert.Equal("Felucca, Trammel, Ilshenar, Malas, Tokuno", mapSelection.ToCommaDelimitedString());
        }

        [Fact]
        public void Can_evaluate_enabled_maps_by_string()
        {
            MapSelection mapSelection = new MapSelection();
            mapSelection.Enable(MapSelectionFlags.Felucca);

            // When
            bool feluccaIsEnabled = mapSelection.Includes("Felucca");
            bool trammelIsEnabled = mapSelection.Includes("Trammel");

            // Then
            Assert.True(feluccaIsEnabled);
            Assert.False(trammelIsEnabled);
        }
    }
}
