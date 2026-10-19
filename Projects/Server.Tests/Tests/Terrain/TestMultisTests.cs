using Xunit;

namespace Server.Tests.Terrain;

[Collection("Sequential Server Tests")]
public class TestMultisTests
{
    [Theory]
    [InlineData(TestMultis.GuildHouse)]
    [InlineData(TestMultis.Tower)]
    [InlineData(TestMultis.SmallBoatNorth)]
    public void Seed_RegistersFootprint(int multiID)
    {
        TestMultis.Seed();

        Assert.NotEmpty(MultiData.GetComponents(multiID).List);
    }

    [Fact]
    public void Footprints_DifferBetweenGuildHouseAndTower()
    {
        TestMultis.Seed();

        var house = MultiData.GetComponents(TestMultis.GuildHouse);
        var tower = MultiData.GetComponents(TestMultis.Tower);
        Assert.NotEqual(house.Width, tower.Width);
    }

    [Fact]
    public void GuildHouse_HasAnOpenDoorway()
    {
        TestMultis.Seed();

        var house = MultiData.GetComponents(TestMultis.GuildHouse);
        var doorway = house.Tiles[house.Center.X][house.Height - 1];
        Assert.Single(doorway);
        Assert.Equal(TestTiles.Floor, doorway[0].ID & TileData.MaxItemValue);
    }
}
