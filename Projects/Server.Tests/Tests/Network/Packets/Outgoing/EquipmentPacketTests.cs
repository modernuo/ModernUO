using System.Collections.Generic;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class EquipmentPacketTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("Some Crafter", false)]
    [InlineData("", true)]
    public void TestDisplayEquipmentInfo(string name, bool unidentified)
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();
        m.RawName = name;

        var item = new Item(World.NewItem);

        var info = new EquipmentInfo(
            500000,
            m,
            unidentified,
            new[]
            {
                new EquipInfoAttribute(500001, 1),
                new EquipInfoAttribute(500002, 2),
                new EquipInfoAttribute(500002, 3)
            }
        );

        PacketAssert.Sends(
            new DisplayEquipmentInfo(item, info),
            ns => ns.SendDisplayEquipmentInfo(
                item.Serial,
                info.Number,
                info.Crafter?.RawName,
                info.Unidentified,
                new List<EquipInfoAttribute>(info.Attributes)
            )
        );
    }

    [Fact]
    public void TestEquipUpdate()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var item = new Item(World.NewItem) { Parent = m };

        PacketAssert.Sends(new EquipUpdate(item), ns => ns.SendEquipUpdate(item));
    }
}
