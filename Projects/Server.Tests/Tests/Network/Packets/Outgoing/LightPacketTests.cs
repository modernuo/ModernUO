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

        PacketAssert.Sends(new GlobalLightLevel(lightLevel), ns => ns.SendGlobalLightLevel(lightLevel));
    }

    [Fact]
    public void TestPersonalLightLevel()
    {
        var serial = (Serial)0x1024;
        byte lightLevel = 5;

        PacketAssert.Sends(
            new PersonalLightLevel(serial, lightLevel),
            ns => ns.SendPersonalLightLevel(serial, lightLevel)
        );
    }
}
