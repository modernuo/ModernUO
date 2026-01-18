using Server;
using Server.Items;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class TestMapItemPackets
{
    [Theory]
    [InlineData(ProtocolChanges.NewCharacterList)]
    [InlineData(ProtocolChanges.None)]
    public void TestSendMapDetails(ProtocolChanges changes)
    {
        var mapItem = new MapItem(Map.Trammel);

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.ProtocolChanges = changes;

        var expected = (ns.NewCharacterList ?
            (Packet)new MapDetailsNew(mapItem) : new MapDetails(mapItem)).Compile();
        ns.SendMapDetails(mapItem);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Theory]
    [InlineData(5, 0, 0, 0)]
    [InlineData(1, 0, 100, 200)]
    [InlineData(7, 1, 0, 0)]
    [InlineData(7, 0, 0, 0)]
    public void TestSendMapCommand(int command, int number, int x, int y)
    {
        var mapItem = new MapItem(Map.Trammel);

        var expected = new MapCommand(mapItem, command, number, x, y).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendMapCommand(mapItem, command, x, y, number > 0);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
