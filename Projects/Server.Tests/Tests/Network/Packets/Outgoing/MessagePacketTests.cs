using Server.Network;
using Server.Prompts;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class MessageTests
{
    [Fact]
    public void TestMessageLocalized()
    {
        var serial = (Serial)0x1024;
        var graphic = 0x100;
        var messageType = MessageType.Label;
        var hue = 1024;
        var font = 3;
        var number = 150000;
        var name = "Stuff";
        var args = "Arguments";

        var expected = new MessageLocalized(
            serial,
            graphic,
            messageType,
            hue,
            font,
            number,
            name,
            args
        ).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendMessageLocalized(
            serial,
            graphic,
            messageType,
            hue,
            font,
            number,
            name,
            args
        );

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestMessageLocalizedAffix()
    {
        var serial = (Serial)0x1024;
        var graphic = 0x100;
        var messageType = MessageType.Label;
        var hue = 1024;
        var font = 3;
        var number = 150000;
        var name = "Stuff";
        var args = "Arguments";
        var affixType = AffixType.System;
        var affix = "Affix";

        var expected = new MessageLocalizedAffix(
            serial,
            graphic,
            messageType,
            hue,
            font,
            number,
            name,
            affixType,
            affix,
            args
        ).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendMessageLocalizedAffix(
            serial,
            graphic,
            messageType,
            hue,
            font,
            number,
            name,
            affixType,
            affix,
            args
        );

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestAsciiMessage()
    {
        var serial = (Serial)0x1024;
        var graphic = 0x100;
        var messageType = MessageType.Label;
        var hue = 1024;
        var font = 3;
        var name = "Stuff";
        var text = "Some Text";

        var expected = new AsciiMessage(
            serial,
            graphic,
            messageType,
            hue,
            font,
            name,
            text
        ).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendMessage(
            serial,
            graphic,
            messageType,
            hue,
            font,
            true,
            null,
            name,
            text
        );

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestUnicodeMessage()
    {
        var serial = (Serial)0x1024;
        var graphic = 0x100;
        var messageType = MessageType.Label;
        var hue = 1024;
        var font = 3;
        var lang = "ENU";
        var name = "Stuff";
        var text = "Some Text";

        var expected = new UnicodeMessage(
            serial,
            graphic,
            messageType,
            hue,
            font,
            lang,
            name,
            text
        ).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendMessage(
            serial,
            graphic,
            messageType,
            hue,
            font,
            false,
            lang,
            name,
            text
        );

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestFollowMessage()
    {
        var serial = (Serial)0x1024;
        var serial2 = (Serial)0x2;

        var expected = new FollowMessage(serial, serial2).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendFollowMessage(serial, serial2);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestObjectHelpResponse()
    {
        var s = (Serial)0x100;
        var text = "This is some testing text";

        var expected = new ObjectHelpResponse(s, text).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendHelpResponse(s, text);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    internal class TestPrompt : Prompt
    {
    }

    [Collection("Sequential Server Tests")]
    public class UnicodePromptTests
    {
        [Fact]
        public void TestUnicodePrompt()
        {
            var prompt = new TestPrompt();
            var expected = new UnicodePrompt(prompt).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPrompt(prompt);

            var result = ns.SendBuffer.GetReadSpan();
            AssertThat.Equal(result, expected);
        }
    }
}
