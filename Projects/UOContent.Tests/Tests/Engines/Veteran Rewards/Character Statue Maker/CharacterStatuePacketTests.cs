using System;
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
            Serial serial = s;
            var expected = new UpdateStatueAnimation(serial, status, anim, frame).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendStatueAnimation(serial, status, anim, frame);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
