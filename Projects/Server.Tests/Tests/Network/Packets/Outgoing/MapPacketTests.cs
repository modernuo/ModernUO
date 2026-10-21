using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class MapPatchesTests
{
    [Fact]
    public void TestMapPatches()
    {
        PacketAssert.Sends(new MapPatches(), ns => ns.SendMapPatches());
    }

    [Fact]
    public void TestInvalidMapEnable()
    {
        PacketAssert.Sends(new InvalidMapEnable(), ns => ns.SendInvalidMap());
    }

    [Theory]
    [InlineData("Felucca")]
    [InlineData("Malas")]
    public void TestMapChange(string mapName)
    {
        var map = Map.Parse(mapName);

        PacketAssert.Sends(new MapChange(map), ns => ns.SendMapChange(map));
    }
}
