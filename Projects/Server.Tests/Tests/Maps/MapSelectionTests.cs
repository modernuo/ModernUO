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
            Assert.False(mapSelection.IsEnabled(MapSelectionFlags.Felucca));
            Assert.False(mapSelection.IsEnabled(MapSelectionFlags.Trammel));
            Assert.False(mapSelection.IsEnabled(MapSelectionFlags.Ilshenar));
            Assert.False(mapSelection.IsEnabled(MapSelectionFlags.Malas));
            Assert.False(mapSelection.IsEnabled(MapSelectionFlags.Tokuno));
            Assert.False(mapSelection.IsEnabled(MapSelectionFlags.TerMur));
        }

        [Fact]
        public void Enable_all_maps()
        {
            MapSelection mapSelection = new MapSelection();

            // When
            mapSelection.EnableAll();

            // Then
            Assert.True(mapSelection.IsEnabled(MapSelectionFlags.Felucca));
            Assert.True(mapSelection.IsEnabled(MapSelectionFlags.Trammel));
            Assert.True(mapSelection.IsEnabled(MapSelectionFlags.Ilshenar));
            Assert.True(mapSelection.IsEnabled(MapSelectionFlags.Malas));
            Assert.True(mapSelection.IsEnabled(MapSelectionFlags.Tokuno));
            Assert.True(mapSelection.IsEnabled(MapSelectionFlags.TerMur));
        }

        [Fact]
        public void Disable_individual_map()
        {
            MapSelection mapSelection = new MapSelection();
            mapSelection.EnableAll();

            // When
            mapSelection.Disable(MapSelectionFlags.Trammel);

            // Then
            Assert.False(mapSelection.IsEnabled(MapSelectionFlags.Trammel));
        }

        [Fact]
        public void Enable_individual_map()
        {
            MapSelection mapSelection = new MapSelection();

            // When
            mapSelection.Enable(MapSelectionFlags.Trammel);

            // Then
            Assert.True(mapSelection.IsEnabled(MapSelectionFlags.Trammel));
        }
    }
}
