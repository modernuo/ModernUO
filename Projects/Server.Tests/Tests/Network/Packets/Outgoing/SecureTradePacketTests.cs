using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class SecureTradePacketTests
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

        PacketAssert.Sends(
            new DisplaySecureTrade(m, firstCont, secondCont, name),
            ns => ns.SendDisplaySecureTrade(m, firstCont, secondCont, name)
        );
    }

    [Fact]
    public void TestCloseSecureTrade()
    {
        var cont = new Container(World.NewItem);

        PacketAssert.Sends(new CloseSecureTrade(cont), ns => ns.SendCloseSecureTrade(cont));
    }

    [Theory, InlineData(true, false), InlineData(false, true)]
    // Update first
    // Update second
    public void TestUpdateSecureTrade(bool first, bool second)
    {
        var firstCont = new Container(World.NewItem);
        var secondCont = new Container(World.NewItem);

        var cont = first ? firstCont : secondCont;

        PacketAssert.Sends(
            new UpdateSecureTrade(cont, first, second),
            ns => ns.SendUpdateSecureTrade(cont, first, second)
        );
    }

    [Theory]
    [InlineData(100000, 30, TradeFlag.UpdateGold)]
    [InlineData(250000, 50000, TradeFlag.UpdateLedger)]
    public void TestUpdateGoldSecureTrade(int gold, int plat, TradeFlag flag)
    {
        var cont = new Container(World.NewItem);

        PacketAssert.Sends(
            new UpdateSecureTrade(cont, flag, gold, plat),
            ns => ns.SendUpdateSecureTrade(cont, flag, gold, plat)
        );
    }

    [Fact]
    public void TestSecureTradeEquip()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var cont = new Container(World.NewItem);
        var itemInCont = new Item(World.NewItem) { Parent = cont };

        PacketAssert.Sends(new SecureTradeEquip(itemInCont, m), ns => ns.SendSecureTradeEquip(itemInCont, m));
    }

    [Fact]
    public void TestSecureTradeEquip6017()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var cont = new Container(World.NewItem);
        var itemInCont = new Item(World.NewItem) { Parent = cont };

        PacketAssert.Sends(
            new SecureTradeEquip6017(itemInCont, m),
            ns => ns.SendSecureTradeEquip(itemInCont, m),
            ProtocolChanges.ContainerGridLines
        );
    }
}
