using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class MapPatchesTests
{
    [Fact]
    public void TestMapPatches()
    {
        using var ns = PacketTestUtilities.CreateTestNetState();

        var expected = new MapPatches().Compile();

        ns.SendMapPatches();

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestInvalidMapEnable()
    {
        var expected = new InvalidMapEnable().Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendInvalidMap();

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Theory]
    [InlineData("Felucca")]
    [InlineData("Malas")]
    public void TestMapChange(string mapName)
    {
        var map = Map.Parse(mapName);
        var expected = new MapChange(map).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendMapChange(map);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
