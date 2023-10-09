using System;
using Server;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
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

            var ns = PacketTestUtilities.CreateTestNetState();
            BuffInfo.SendAddBuffPacket(ns, (Serial)mob, iconID, titleCliloc, secondaryCliloc, args, (int)timeSpan.TotalMilliseconds);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestRemoveBuffIcon()
        {
            Serial m = (Serial)0x1024;
            var buffIcon = BuffIcon.Disguised;
            var expected = new RemoveBuffPacket(m, buffIcon).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            BuffInfo.SendRemoveBuffPacket(ns, m, buffIcon);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }
    }
}
