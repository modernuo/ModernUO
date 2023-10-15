using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    [Collection("Sequential Tests")]
    public class ContainerPacketTests : IClassFixture<ServerFixture>
    {

        [Fact]
        public void TestContainerDisplay()
        {
            Serial serial = (Serial)0x1024;
            ushort gumpId = 100;

            var expected = new ContainerDisplay(serial, gumpId).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplayContainer(serial, gumpId);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestContainerDisplayHS()
        {
            Serial serial = (Serial)0x1024;
            ushort gumpId = 100;

            var expected = new ContainerDisplayHS(serial, gumpId).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ns.ProtocolChanges | ProtocolChanges.ContainerGridLines | ProtocolChanges.HighSeas;
            ns.SendDisplayContainer(serial, gumpId);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestDisplaySpellbook()
        {
            Serial serial = (Serial)0x1024;

            var expected = new DisplaySpellbook(serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplaySpellbook(serial);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestDisplaySpellbookHS()
        {
            Serial serial = (Serial)0x1024;

            var expected = new DisplaySpellbookHS(serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ns.ProtocolChanges | ProtocolChanges.ContainerGridLines | ProtocolChanges.HighSeas;
            ns.SendDisplaySpellbook(serial);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestNewSpellbookContent()
        {
            Serial serial = (Serial)0x1024;
            ushort graphic = 100;
            ushort offset = 10;
            ulong content = 0x123456789ABCDEF0;
            bool opl = ObjectPropertyList.Enabled;

            var expected = new NewSpellbookContent(serial, graphic, offset, content).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ns.ProtocolChanges | ProtocolChanges.ContainerGridLines | ProtocolChanges.NewSpellbook;
            ObjectPropertyList.Enabled = true;
            ns.SendSpellbookContent(serial, graphic, offset, content);
            ObjectPropertyList.Enabled = opl;

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestSpellbookContent()
        {
            Serial serial = (Serial)0x1024;
            ushort offset = 10;
            ushort graphic = 100;
            ulong content = 0x123456789ABCDEF0;

            var expected = new SpellbookContent(serial, offset, content).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSpellbookContent(serial, graphic, offset, content);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestSpellbookContent6017()
        {
            Serial serial = (Serial)0x1024;
            ushort offset = 10;
            ushort graphic = 100;
            ulong content = 0x123456789ABCDEF0;

            var expected = new SpellbookContent6017(serial, offset, content).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges |= ProtocolChanges.ContainerGridLines;
            ns.SendSpellbookContent(serial, graphic, offset, content);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestContainerContentUpdate()
        {
            Serial serial = (Serial)0x1024;
            var item = new Item(serial);

            var expected = new ContainerContentUpdate(item).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendContainerContentUpdate(item);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestContainerContentUpdate6017()
        {
            Serial serial = (Serial)0x1024;
            var item = new Item(serial);

            var expected = new ContainerContentUpdate6017(item).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges |= ProtocolChanges.ContainerGridLines;
            ns.SendContainerContentUpdate(item);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestContainerContent()
        {
            var cont = new Container(World.NewItem);
            cont.AddItem(new Item(World.NewItem));
            cont.Map = Map.Felucca;

            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.AccessLevel = AccessLevel.Administrator;
            m.Map = Map.Felucca;

            var expected = new ContainerContent(m, cont).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendContainerContent(m, cont);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestContainerContent6017()
        {
            var cont = new Container(World.NewItem);
            cont.AddItem(new Item(World.NewItem));
            cont.Map = Map.Felucca;

            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.AccessLevel = AccessLevel.Administrator;
            m.Map = Map.Felucca;

            var expected = new ContainerContent6017(m, cont).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges |= ProtocolChanges.ContainerGridLines;
            ns.SendContainerContent(m, cont);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
