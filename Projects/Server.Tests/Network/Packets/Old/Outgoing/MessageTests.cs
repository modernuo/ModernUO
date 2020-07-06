using System;
using Xunit;
using Server.Network;

namespace Server.Tests.Network.Packets
{
  public class MessageTests
  {
    [Fact]
    public void TestMessageLocalized()
    {
      Serial serial = 0x1;
      int graphic = 0x100;
      var messageType = MessageType.Label;
      int hue = 1024;
      int font = 3;
      int number = 150000;
      string name = "Stuff";
      string args = "Arguments";

      Span<byte> data = new MessageLocalized(
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
      int pos = 0;
      ((byte)0xC1).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length

      serial.CopyTo(ref pos, expectedData);
      ((ushort)graphic).CopyTo(ref pos, expectedData);
      ((byte)messageType).CopyTo(ref pos, expectedData);
      ((ushort)hue).CopyTo(ref pos, expectedData);
      ((ushort)font).CopyTo(ref pos, expectedData);
      number.CopyTo(ref pos, expectedData);
      name.CopyASCIIFixedTo(ref pos, 30, expectedData);
      args.CopyRawUnicodeLittleEndianTo(ref pos, expectedData);
      expectedData.Clear(ref pos, 2); // Terminator

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestMessageLocalizedAffix()
    {
      Serial serial = 0x1;
      int graphic = 0x100;
      var messageType = MessageType.Label;
      int hue = 1024;
      int font = 3;
      int number = 150000;
      string name = "Stuff";
      string args = "Arguments";
      var affixType = AffixType.System;
      string affix = "Affix";

      Span<byte> data = new MessageLocalizedAffix(
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
      int pos = 0;
      ((byte)0xCC).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length

      serial.CopyTo(ref pos, expectedData);
      ((ushort)graphic).CopyTo(ref pos, expectedData);
      ((byte)messageType).CopyTo(ref pos, expectedData);
      ((ushort)hue).CopyTo(ref pos, expectedData);
      ((ushort)font).CopyTo(ref pos, expectedData);
      number.CopyTo(ref pos, expectedData);
      ((byte)affixType).CopyTo(ref pos, expectedData);
      name.CopyASCIIFixedTo(ref pos, 30, expectedData);
      affix.CopyRawASCIITo(ref pos, expectedData);
      expectedData.Clear(ref pos, 2); // Terminator
      args.CopyRawUnicodeLittleEndianTo(ref pos, expectedData);
      expectedData.Clear(ref pos, 1); // Terminator

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestAsciiMessage()
    {
      Serial serial = 0x1;
      int graphic = 0x100;
      var messageType = MessageType.Label;
      int hue = 1024;
      int font = 3;
      string name = "Stuff";
      string text = "Some Text";

      Span<byte> data = new AsciiMessage(
        serial,
        graphic,
        messageType,
        hue,
        font,
        name,
        text
      ).Compile();

      Span<byte> expectedData = stackalloc byte[45 + text.Length];
      int pos = 0;
      ((byte)0x1C).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length

      serial.CopyTo(ref pos, expectedData);
      ((ushort)graphic).CopyTo(ref pos, expectedData);
      ((byte)messageType).CopyTo(ref pos, expectedData);
      ((ushort)hue).CopyTo(ref pos, expectedData);
      ((ushort)font).CopyTo(ref pos, expectedData);
      name.CopyASCIIFixedTo(ref pos, 30, expectedData);
      text.CopyRawASCIITo(ref pos, expectedData);
      expectedData.Clear(ref pos, 1); // Terminator

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestUnicodeMessage()
    {
      Serial serial = 0x1;
      int graphic = 0x100;
      var messageType = MessageType.Label;
      int hue = 1024;
      int font = 3;
      string lang = "ENU";
      string name = "Stuff";
      string text = "Some Text";

      Span<byte> data = new UnicodeMessage(
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
      int pos = 0;
      ((byte)0xAE).CopyTo(ref pos, expectedData); // Packet ID
      ((ushort)expectedData.Length).CopyTo(ref pos, expectedData); // Length

      serial.CopyTo(ref pos, expectedData);
      ((ushort)graphic).CopyTo(ref pos, expectedData);
      ((byte)messageType).CopyTo(ref pos, expectedData);
      ((ushort)hue).CopyTo(ref pos, expectedData);
      ((ushort)font).CopyTo(ref pos, expectedData);
      lang.CopyASCIIFixedTo(ref pos, 4, expectedData);
      name.CopyASCIIFixedTo(ref pos, 30, expectedData);
      text.CopyRawUnicodeBigEndianTo(ref pos, expectedData);
      expectedData.Clear(ref pos, 2); // Terminator

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestFollowMessage()
    {
      Serial serial = 0x1;
      Serial serial2 = 0x2;

      Span<byte> data = new FollowMessage(serial, serial2).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x15, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Follower
        0x00, 0x00, 0x00, 0x00, // Followee
      };

      serial.CopyTo(expectedData.Slice(1, 4));
      serial2.CopyTo(expectedData.Slice(5, 4));

      AssertThat.Equal(data, expectedData);
    }
  }
}
