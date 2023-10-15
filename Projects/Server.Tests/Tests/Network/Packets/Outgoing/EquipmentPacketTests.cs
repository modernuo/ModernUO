using System.Collections.Generic;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class EquipmentPacketTests : IClassFixture<ServerFixture>
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

            var expected = new DisplayEquipmentInfo(item, info).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplayEquipmentInfo(
                item.Serial,
                info.Number,
                info.Crafter?.RawName,
                info.Unidentified,
                new List<EquipInfoAttribute>(info.Attributes)
            );

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestEquipUpdate()
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();

            var item = new Item(World.NewItem) { Parent = m };

            var expected = new EquipUpdate(item).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendEquipUpdate(item);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
