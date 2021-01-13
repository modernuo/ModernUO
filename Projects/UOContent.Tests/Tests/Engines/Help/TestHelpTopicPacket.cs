using System;
using Server.Engines.Help;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
    public class TestHelpTopicPacket
    {
        [Theory]
        [InlineData(HelpTopic.HEALING, false)]
        [InlineData(HelpTopic.EmptyingBowl, true)]
        public void TestDisplayHelpTopic(int topic, bool display)
        {
            var expected = new DisplayHelpTopic(topic, display).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplayHelpTopic(topic, display);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
