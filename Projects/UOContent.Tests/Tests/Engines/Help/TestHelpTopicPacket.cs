using Server.Engines.Help;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class TestHelpTopicPacket
{
    [Theory]
    [InlineData(HelpTopic.Healing, false)]
    [InlineData(HelpTopic.EmptyingBowl, true)]
    public void TestDisplayHelpTopic(int topic, bool display)
    {
        var expected = new DisplayHelpTopic(topic, display).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendDisplayHelpTopic(topic, display);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
