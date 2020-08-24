using System;
using System.Buffers;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class SecureTradePacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData("short-name")]
        [InlineData("this is a really long name that is more than 30 characters, probably")]
        public void TestDisplaySecureTrade(string name)
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var firstCont = new Container(Serial.LastItem + 1);
            var secondCont = new Container(Serial.LastItem + 2);

            Span<byte> data = new DisplaySecureTrade(m, firstCont, secondCont, name).Compile();

            bool hasName = name.Length > 0;

            Span<byte> expectedData = stackalloc byte[17 + (hasName ? 30 : 0)];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x6F); // Packet ID
            expectedData.Write(ref pos, (ushort)0x2F); // Length
            expectedData.Write(ref pos, (byte)TradeFlag.Display); // Command
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, firstCont.Serial);
            expectedData.Write(ref pos, secondCont.Serial);
            expectedData.Write(ref pos, hasName);
            if (hasName)
                expectedData.WriteAsciiFixed(ref pos, name, 30);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestCloseSecureTrade()
        {
            var cont = new Container(Serial.LastItem + 1);

            Span<byte> data = new CloseSecureTrade(cont).Compile();

            Span<byte> expectedData = stackalloc byte[8];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x6F); // Packet ID
            expectedData.Write(ref pos, (ushort)0x8); // Length
            expectedData.Write(ref pos, (byte)TradeFlag.Close); // Command
            expectedData.Write(ref pos, cont.Serial);

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(true, false)] // Update first
        [InlineData(false, true)] // Update second
        public void TestUpdateSecureTrade(bool first, bool second)
        {
            var firstCont = new Container(Serial.LastItem + 1);
            var secondCont = new Container(Serial.LastItem + 2);

            Container cont = first ? firstCont : secondCont;
            Span<byte> data = new UpdateSecureTrade(cont, first, second).Compile();

            Span<byte> expectedData = stackalloc byte[16];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x6F); // Packet ID
            expectedData.Write(ref pos, (ushort)0x10); // Length
            expectedData.Write(ref pos, (byte)TradeFlag.Update); // Command
            expectedData.Write(ref pos, cont.Serial);
            expectedData.Write(ref pos, first ? 1 : 0); // true if first
            expectedData.Write(ref pos, second ? 1 : 0); // true if second

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(100000, 30, TradeFlag.UpdateGold)]
        [InlineData(250000, 50000, TradeFlag.UpdateLedger)]
        public void TestUpdateGoldSecureTrade(int gold, int plat, TradeFlag flag)
        {
            var cont = new Container(Serial.LastItem + 1);
            Span<byte> data = new UpdateSecureTrade(cont, flag, gold, plat).Compile();

            Span<byte> expectedData = stackalloc byte[16];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x6F); // Packet ID
            expectedData.Write(ref pos, (ushort)0x10); // Length
            expectedData.Write(ref pos, (byte)flag); // Command
            expectedData.Write(ref pos, cont.Serial);
            expectedData.Write(ref pos, gold);
            expectedData.Write(ref pos, plat);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestSecureTradeEquip()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var cont = new Container(Serial.LastItem + 1);
            var itemInCont = new Item(Serial.LastItem + 2) { Parent = cont };

            Span<byte> data = new SecureTradeEquip(itemInCont, m).Compile();
            Span<byte> expectedData = stackalloc byte[20];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x25); // Packet ID
            expectedData.Write(ref pos, itemInCont.Serial);
            expectedData.Write(ref pos, (short)itemInCont.ItemID);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif
            expectedData.Write(ref pos, (short)itemInCont.Amount);
            expectedData.Write(ref pos, (short)itemInCont.X);
            expectedData.Write(ref pos, (short)itemInCont.Y);
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, (short)itemInCont.Hue);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestSecureTradeEquip6017()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var cont = new Container(Serial.LastItem + 1);
            var itemInCont = new Item(Serial.LastItem + 2) { Parent = cont };

            Span<byte> data = new SecureTradeEquip6017(itemInCont, m).Compile();

            Span<byte> expectedData = stackalloc byte[21];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x25); // Packet ID
            expectedData.Write(ref pos, itemInCont.Serial);
            expectedData.Write(ref pos, (short)itemInCont.ItemID);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif
            expectedData.Write(ref pos, (short)itemInCont.Amount);
            expectedData.Write(ref pos, (short)itemInCont.X);
            expectedData.Write(ref pos, (short)itemInCont.Y);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#else
            pos++;
#endif
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, (short)itemInCont.Hue);

            AssertThat.Equal(data, expectedData);
        }
    }
}
