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

        PacketAssert.Sends(
            new MessageLocalized(serial, graphic, messageType, hue, font, number, name, args),
            ns => ns.SendMessageLocalized(serial, graphic, messageType, hue, font, number, name, args)
        );
    }

    [Fact]
    public void TestMessageLocalizedInterpolated()
    {
        var serial = (Serial)0x1024;
        var graphic = 0x100;
        var messageType = MessageType.Label;
        var hue = 1024;
        var font = 3;
        var number = 150000;
        var name = "Stuff";
        var argValue = 42;

        PacketAssert.Sends(
            new MessageLocalized(serial, graphic, messageType, hue, font, number, name, $"value: {argValue}"),
            ns => ns.SendMessageLocalized(serial, graphic, messageType, hue, font, number, name, $"value: {argValue}")
        );
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

        PacketAssert.Sends(
            new MessageLocalizedAffix(serial, graphic, messageType, hue, font, number, name, affixType, affix, args),
            ns => ns.SendMessageLocalizedAffix(
                serial, graphic, messageType, hue, font, number, name, affixType, affix, args
            )
        );
    }

    [Fact]
    public void TestMessageLocalizedAffixInterpolated()
    {
        var serial = (Serial)0x1024;
        var graphic = 0x100;
        var messageType = MessageType.Label;
        var hue = 1024;
        var font = 3;
        var number = 150000;
        var name = "Stuff";
        var affixType = AffixType.System;
        var affix = "Affix";
        var argValue = 42;

        PacketAssert.Sends(
            new MessageLocalizedAffix(
                serial, graphic, messageType, hue, font, number, name, affixType, affix, $"value: {argValue}"
            ),
            ns => ns.SendMessageLocalizedAffix(
                serial, graphic, messageType, hue, font, number, name, affixType, affix, $"value: {argValue}"
            )
        );
    }

    [Fact]
    public void TestUnicodeMessageInterpolated()
    {
        var serial = (Serial)0x1024;
        var graphic = 0x100;
        var messageType = MessageType.Label;
        var hue = 1024;
        var font = 3;
        var lang = "ENU";
        var name = "Stuff";
        var who = "Alice";

        PacketAssert.Sends(
            new UnicodeMessage(serial, graphic, messageType, hue, font, lang, name, $"hello, {who}"),
            ns => ns.SendMessage(serial, graphic, messageType, hue, font, false, lang, name, $"hello, {who}")
        );
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

        PacketAssert.Sends(
            new AsciiMessage(serial, graphic, messageType, hue, font, name, text),
            ns => ns.SendMessage(serial, graphic, messageType, hue, font, true, null, name, text)
        );
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

        PacketAssert.Sends(
            new UnicodeMessage(serial, graphic, messageType, hue, font, lang, name, text),
            ns => ns.SendMessage(serial, graphic, messageType, hue, font, false, lang, name, text)
        );
    }

    [Fact]
    public void TestFollowMessage()
    {
        var serial = (Serial)0x1024;
        var serial2 = (Serial)0x2;

        PacketAssert.Sends(new FollowMessage(serial, serial2), ns => ns.SendFollowMessage(serial, serial2));
    }

    [Fact]
    public void TestObjectHelpResponse()
    {
        var s = (Serial)0x100;
        var text = "This is some testing text";

        PacketAssert.Sends(new ObjectHelpResponse(s, text), ns => ns.SendHelpResponse(s, text));
    }

    [Fact]
    public void TestUnicodePrompt()
    {
        var prompt = new TestPrompt();

        PacketAssert.Sends(new UnicodePrompt(prompt), ns => ns.SendPrompt(prompt));
    }

    private class TestPrompt : Prompt
    {
    }
}
