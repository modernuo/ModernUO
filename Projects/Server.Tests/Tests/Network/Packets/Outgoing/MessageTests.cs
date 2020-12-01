using System;
using Server.Network;
using Server.Prompts;
using Xunit;

namespace Server.Tests.Network
{
    public class MessageTests
    {
        [Fact]
        public void TestMessageLocalized()
        {
            Serial serial = 0x1;
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

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMessageLocalizedAffix()
        {
            Serial serial = 0x1;
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

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestAsciiMessage()
        {
            Serial serial = 0x1;
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

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestUnicodeMessage()
        {
            Serial serial = 0x1;
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

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestFollowMessage()
        {
            Serial serial = 0x1;
            Serial serial2 = 0x2;

            var expected = new FollowMessage(serial, serial2).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendFollowMessage(serial, serial2);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        internal class TestPrompt : Prompt
        {
        }

        public class UnicodePromptTests
        {
            [Fact]
            public void TestUnicodePrompt()
            {
                var prompt = new TestPrompt();
                var expected = new UnicodePrompt(prompt).Compile();

                using var ns = PacketTestUtilities.CreateTestNetState();
                ns.SendPrompt(prompt);

                var result = ns.SendPipe.Reader.TryRead();
                AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);

            }
        }
    }
}
