using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class SecureTradePacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData("short-name")]
        [InlineData("this is a really long name that is more than 30 characters, probably")]
        public void TestDisplaySecureTrade(string name)
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();

            var firstCont = new Container(World.NewItem);
            var secondCont = new Container(World.NewItem);

            var expected = new DisplaySecureTrade(m, firstCont, secondCont, name).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplaySecureTrade(m, firstCont, secondCont, name);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestCloseSecureTrade()
        {
            var cont = new Container(World.NewItem);

            var expected = new CloseSecureTrade(cont).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendCloseSecureTrade(cont);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory, InlineData(true, false), InlineData(false, true)]
        // Update first
         // Update second
        public void TestUpdateSecureTrade(bool first, bool second)
        {
            var firstCont = new Container(World.NewItem);
            var secondCont = new Container(World.NewItem);

            var cont = first ? firstCont : secondCont;
            var expected = new UpdateSecureTrade(cont, first, second).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendUpdateSecureTrade(cont, first, second);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(100000, 30, TradeFlag.UpdateGold)]
        [InlineData(250000, 50000, TradeFlag.UpdateLedger)]
        public void TestUpdateGoldSecureTrade(int gold, int plat, TradeFlag flag)
        {
            var cont = new Container(World.NewItem);
            var expected = new UpdateSecureTrade(cont, flag, gold, plat).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendUpdateSecureTrade(cont, flag, gold, plat);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestSecureTradeEquip()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();

            var cont = new Container(World.NewItem);
            var itemInCont = new Item(World.NewItem) { Parent = cont };

            var expected = new SecureTradeEquip(itemInCont, m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSecureTradeEquip(itemInCont, m);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestSecureTradeEquip6017()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();

            var cont = new Container(World.NewItem);
            var itemInCont = new Item(World.NewItem) { Parent = cont };

            var expected = new SecureTradeEquip6017(itemInCont, m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges |= ProtocolChanges.ContainerGridLines;

            ns.SendSecureTradeEquip(itemInCont, m);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
