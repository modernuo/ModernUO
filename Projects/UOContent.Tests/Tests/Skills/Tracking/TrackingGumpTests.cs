using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

public class TrackingGumpTests : IClassFixture<ServerFixture>
{
    [Fact]
    // Regression test used to identify an issue with AddItem compilation of the packet
    public void TestTrackingGump()
    {
        var pm = new PlayerMobile();
        pm.Skills.Tracking.BaseFixedPoint = 1000;
        var g = new TrackWhatGump(pm);

        var ns = PacketTestUtilities.CreateTestNetState();

        var expected = g.Compile(ns).Compile();
        ns.SendDisplayGump(g, out var switches, out var entries);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }
}
