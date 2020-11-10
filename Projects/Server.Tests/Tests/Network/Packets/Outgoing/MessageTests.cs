using System;
using System.Buffers;
using Server.Network;
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

            var data = new MessageLocalized(
                serial,
                graphic,
                messageType,
                hue,
                font,
                number,
                name,
                args
            ).Compile();

            Span<byte> expectedData = stackalloc byte[50 + args.Length * 2];
            var pos = 0;
            expectedData.Write(ref pos, (byte)0xC1);                  // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length

            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, (ushort)graphic);
            expectedData.Write(ref pos, (byte)messageType);
            expectedData.Write(ref pos, (ushort)hue);
            expectedData.Write(ref pos, (ushort)font);
            expectedData.Write(ref pos, number);
            expectedData.WriteAsciiFixed(ref pos, name, 30);
            expectedData.WriteLittleUniNull(ref pos, args);

            AssertThat.Equal(data, expectedData);
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

            var data = new MessageLocalizedAffix(
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

            Span<byte> expectedData = stackalloc byte[52 + affix.Length + args.Length * 2];
            var pos = 0;
            expectedData.Write(ref pos, (byte)0xCC);                  // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length

            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, (ushort)graphic);
            expectedData.Write(ref pos, (byte)messageType);
            expectedData.Write(ref pos, (ushort)hue);
            expectedData.Write(ref pos, (ushort)font);
            expectedData.Write(ref pos, number);
            expectedData.Write(ref pos, (byte)affixType);
            expectedData.WriteAsciiFixed(ref pos, name, 30);
            expectedData.WriteAsciiNull(ref pos, affix);
            expectedData.WriteBigUniNull(ref pos, args);

            AssertThat.Equal(data, expectedData);
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

            var data = new AsciiMessage(
                serial,
                graphic,
                messageType,
                hue,
                font,
                name,
                text
            ).Compile();

            Span<byte> expectedData = stackalloc byte[45 + text.Length];
            var pos = 0;
            expectedData.Write(ref pos, (byte)0x1C);                  // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length

            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, (ushort)graphic);
            expectedData.Write(ref pos, (byte)messageType);
            expectedData.Write(ref pos, (ushort)hue);
            expectedData.Write(ref pos, (ushort)font);
            expectedData.WriteAsciiFixed(ref pos, name, 30);
            expectedData.WriteAsciiNull(ref pos, text);

            AssertThat.Equal(data, expectedData);
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

            var data = new UnicodeMessage(
                serial,
                graphic,
                messageType,
                hue,
                font,
                lang,
                name,
                text
            ).Compile();

            Span<byte> expectedData = stackalloc byte[50 + text.Length * 2];
            var pos = 0;
            expectedData.Write(ref pos, (byte)0xAE);                  // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length

            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, (ushort)graphic);
            expectedData.Write(ref pos, (byte)messageType);
            expectedData.Write(ref pos, (ushort)hue);
            expectedData.Write(ref pos, (ushort)font);
            expectedData.WriteAsciiFixed(ref pos, lang, 4);
            expectedData.WriteAsciiFixed(ref pos, name, 30);
            expectedData.WriteBigUniNull(ref pos, text);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestFollowMessage()
        {
            Serial serial = 0x1;
            Serial serial2 = 0x2;

            var data = new FollowMessage(serial, serial2).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x15); // Packet ID
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, serial2);

            AssertThat.Equal(data, expectedData);
        }
    }
}
