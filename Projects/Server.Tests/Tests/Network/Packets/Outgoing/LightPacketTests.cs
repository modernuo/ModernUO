using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class LightPacketTests
{
    [Fact]
    public void TestGlobalLightLevel()
    {
        const byte lightLevel = 5;
        var expected = new GlobalLightLevel(lightLevel).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendGlobalLightLevel(lightLevel);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestPersonalLightLevel()
    {
        var serial = (Serial)0x1024;
        byte lightLevel = 5;
        var expected = new PersonalLightLevel(serial, lightLevel).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendPersonalLightLevel(serial, lightLevel);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
