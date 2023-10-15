using Server;
using Server.Engines.VeteranRewards;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
    public class CharacterStatuePacketTests
    {
        [Theory]
        [InlineData(0x1024u, 1, 100, 200)]
        public void TestSendStatueAnimation(uint s, int status, int anim, int frame)
        {
            var expected = new UpdateStatueAnimation((Serial)s, status, anim, frame).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendStatueAnimation((Serial)s, status, anim, frame);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }
    }
}
