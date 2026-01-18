using System;
using Server;
using Server.Engines.BuffIcons;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class BuffIconPacketTests
{
    [Theory]
    [InlineData(0x1024u, BuffIcon.Clumsy, 500100, 300200, "123456", 8000)]
    [InlineData(0x2048u, BuffIcon.Disguised, 500102, 300203, null, 9000)]
    public void TestAddBuffIcon(uint mob, BuffIcon iconID, int titleCliloc, int secondaryCliloc, string args, int ts)
    {
        var timeSpan = new TimeSpan(ts);
        var expected = new AddBuffPacket(
            (Serial)mob, iconID, titleCliloc, secondaryCliloc, args, timeSpan
        ).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendAddBuffPacket((Serial)mob, iconID, titleCliloc, secondaryCliloc, args, (int)timeSpan.TotalMilliseconds);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestRemoveBuffIcon()
    {
        var m = (Serial)0x1024;
        var buffIcon = BuffIcon.Disguised;
        var expected = new RemoveBuffPacket(m, buffIcon).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendRemoveBuffPacket(m, buffIcon);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
