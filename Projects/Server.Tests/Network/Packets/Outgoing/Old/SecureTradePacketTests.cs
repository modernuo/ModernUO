using System;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
  public class SecureTradePacketTests : IClassFixture<ServerFixture>
  {
    private readonly Mobile to;
    private readonly Container firstCont;
    private readonly Container secondCont;
    private readonly Item itemInFirstCont;

    public SecureTradePacketTests(ServerFixture fixture)
    {
      to = fixture.toMobile;
      firstCont = fixture.fromCont;
      secondCont = fixture.toCont;
      itemInFirstCont = fixture.itemInFromCont;
    }

    [Theory]
    [InlineData("short-name")]
    [InlineData("this is a really long name that is more than 30 characters, probably")]
    public void TestDisplaySecureTrade(string name)
    {
      Span<byte> data = new DisplaySecureTrade(to, firstCont, secondCont, name).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x6F, // Packet ID
        0x00, 0x2F, // Length
        (byte)TradeFlag.Display, // Command
        0x00, 0x00, 0x00, 0x00, // Mobile Serial
        0x00, 0x00, 0x00, 0x00, // First Container Serial
        0x00, 0x00, 0x00, 0x00, // Second Container Serial
        0x01, // true if has name
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Name
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
      };

      int pos = 4;
      to.Serial.CopyTo(ref pos, expectedData);
      firstCont.Serial.CopyTo(ref pos, expectedData);
      secondCont.Serial.CopyTo(ref pos, expectedData);
      pos++;
      name.CopyASCIITo(ref pos, 30, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestCloseSecureTrade()
    {
      Span<byte> data = new CloseSecureTrade(firstCont).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x6F, // Packet ID
        0x00, 0x8, // Length
        (byte)TradeFlag.Close, // Command
        0x00, 0x00, 0x00, 0x00 // Container Serial
      };

      firstCont.Serial.CopyTo(expectedData.Slice(4, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(true, false)] // Update first
    [InlineData(false, true)] // Update second
    public void TestUpdateSecureTrade(bool first, bool second)
    {
      Container cont = first ? firstCont : secondCont;
      Span<byte> data = new UpdateSecureTrade(cont, first, second).Compile();

      Span<byte> expectedData = stackalloc byte[]
      {
        0x6F, // Packet ID
        0x00, 0x10, // Length
        (byte)TradeFlag.Update, // Command
        0x00, 0x00, 0x00, 0x00, // Container Serial
        0x00, 0x00, 0x00, first ? (byte)0x01 : (byte)0x00, // (int)1 if first
        0x00, 0x00, 0x00, second ? (byte)0x01 : (byte)0x00 // (int)1 if second
      };

      cont.Serial.CopyTo(expectedData.Slice(4, 4));

      AssertThat.Equal(data, expectedData);
    }

    [Theory]
    [InlineData(100000, 30, TradeFlag.UpdateGold)]
    [InlineData(250000, 50000, TradeFlag.UpdateLedger)]
    public void TestUpdateGoldSecureTrade(int gold, int plat, TradeFlag flag)
    {
      Span<byte> data = new UpdateSecureTrade(firstCont, flag, gold, plat).Compile();
      Span<byte> expectedData = stackalloc byte[]
      {
        0x6F, // Packet ID
        0x00, 0x10, // Length
        (byte)flag, // Command
        0x00, 0x00, 0x00, 0x00, // Container Serial
        0x00, 0x00, 0x00, 0x00, // Gold
        0x00, 0x00, 0x00, 0x00 // Platinum
      };

      int pos = 4;
      firstCont.Serial.CopyTo(ref pos, expectedData);
      gold.CopyTo(ref pos, expectedData);
      plat.CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestSecureTradeEquip()
    {
      Span<byte> data = new SecureTradeEquip(itemInFirstCont, to).Compile();
      Span<byte> expectedData = stackalloc byte[]
      {
        0x25, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Item Serial
        0x00, 0x00, // Item ItemID
        0x00,
        0x00, 0x00, // Item Amount
        0x00, 0x00, // X
        0x00, 0x00, // Y
        0x00, 0x00, 0x00, 0x00, // Mobile Serial
        0x00, 0x00 // Item Hue
      };

      int pos = 1;
      itemInFirstCont.Serial.CopyTo(ref pos, expectedData);
      ((short)itemInFirstCont.ItemID).CopyTo(ref pos, expectedData);
      pos++;
      ((short)itemInFirstCont.Amount).CopyTo(ref pos, expectedData);
      ((short)itemInFirstCont.X).CopyTo(ref pos, expectedData);
      ((short)itemInFirstCont.Y).CopyTo(ref pos, expectedData);
      to.Serial.CopyTo(ref pos, expectedData);
      ((short)itemInFirstCont.Hue).CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }

    [Fact]
    public void TestSecureTradeEquip6017()
    {
      Span<byte> data = new SecureTradeEquip6017(itemInFirstCont, to).Compile();
      Span<byte> expectedData = stackalloc byte[]
      {
        0x25, // Packet ID
        0x00, 0x00, 0x00, 0x00, // Item Serial
        0x00, 0x00, // Item ItemID
        0x00, // Unknown
        0x00, 0x00, // Item Amount
        0x00, 0x00, // X
        0x00, 0x00, // Y
        0x00, // Grid Location
        0x00, 0x00, 0x00, 0x00, // Mobile Serial
        0x00, 0x00 // Item Hue
      };

      int pos = 1;
      itemInFirstCont.Serial.CopyTo(ref pos, expectedData);
      ((short)itemInFirstCont.ItemID).CopyTo(ref pos, expectedData);
      pos++;
      ((short)itemInFirstCont.Amount).CopyTo(ref pos, expectedData);
      ((short)itemInFirstCont.X).CopyTo(ref pos, expectedData);
      ((short)itemInFirstCont.Y).CopyTo(ref pos, expectedData);
      pos++;
      to.Serial.CopyTo(ref pos, expectedData);
      ((short)itemInFirstCont.Hue).CopyTo(ref pos, expectedData);

      AssertThat.Equal(data, expectedData);
    }
  }
}
