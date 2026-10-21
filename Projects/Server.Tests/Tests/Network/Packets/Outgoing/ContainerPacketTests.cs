using System.Buffers.Binary;
using Server.Items;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class ContainerPacketTests
{
    [Fact]
    public void TestContainerDisplay()
    {
        var serial = (Serial)0x1024;
        ushort gumpId = 100;

        PacketAssert.Sends(new ContainerDisplay(serial, gumpId), ns => ns.SendDisplayContainer(serial, gumpId));
    }

    [Fact]
    public void TestContainerDisplayHS()
    {
        var serial = (Serial)0x1024;
        ushort gumpId = 100;

        PacketAssert.Sends(
            new ContainerDisplayHS(serial, gumpId),
            ns => ns.SendDisplayContainer(serial, gumpId),
            ProtocolChanges.ContainerGridLines | ProtocolChanges.HighSeas
        );
    }

    [Fact]
    public void TestDisplaySpellbook()
    {
        var serial = (Serial)0x1024;

        PacketAssert.Sends(new DisplaySpellbook(serial), ns => ns.SendDisplaySpellbook(serial));
    }

    [Fact]
    public void TestDisplaySpellbookHS()
    {
        var serial = (Serial)0x1024;

        PacketAssert.Sends(
            new DisplaySpellbookHS(serial),
            ns => ns.SendDisplaySpellbook(serial),
            ProtocolChanges.ContainerGridLines | ProtocolChanges.HighSeas
        );
    }

    [Fact]
    public void TestNewSpellbookContent()
    {
        var serial = (Serial)0x1024;
        ushort graphic = 100;
        ushort offset = 10;
        ulong content = 0x123456789ABCDEF0;
        var opl = ObjectPropertyList.Enabled;
        ObjectPropertyList.Enabled = true;

        try
        {
            PacketAssert.Sends(
                new NewSpellbookContent(serial, graphic, offset, content),
                ns => ns.SendSpellbookContent(serial, graphic, offset, content),
                ProtocolChanges.ContainerGridLines | ProtocolChanges.NewSpellbook
            );
        }
        finally
        {
            ObjectPropertyList.Enabled = opl;
        }
    }

    [Fact]
    public void TestSpellbookContent()
    {
        var serial = (Serial)0x1024;
        ushort offset = 10;
        ushort graphic = 100;
        ulong content = 0x123456789ABCDEF0;

        PacketAssert.Sends(
            new SpellbookContent(serial, offset, content),
            ns => ns.SendSpellbookContent(serial, graphic, offset, content)
        );
    }

    [Fact]
    public void TestSpellbookContent6017()
    {
        var serial = (Serial)0x1024;
        ushort offset = 10;
        ushort graphic = 100;
        ulong content = 0x123456789ABCDEF0;

        PacketAssert.Sends(
            new SpellbookContent6017(serial, offset, content),
            ns => ns.SendSpellbookContent(serial, graphic, offset, content),
            ProtocolChanges.ContainerGridLines
        );
    }

    [Fact]
    public void TestContainerContentUpdate()
    {
        var serial = (Serial)0x1024;
        var item = new Item(serial);

        PacketAssert.Sends(new ContainerContentUpdate(item), ns => ns.SendContainerContentUpdate(item));
    }

    [Fact]
    public void TestContainerContentUpdate6017()
    {
        var serial = (Serial)0x1024;
        var item = new Item(serial);

        PacketAssert.Sends(
            new ContainerContentUpdate6017(item),
            ns => ns.SendContainerContentUpdate(item),
            ProtocolChanges.ContainerGridLines
        );
    }

    // An administrator beholder on the container's map, so every item is visible and sent.
    private static (Container Container, Mobile Beholder) CreateViewedContainer(int itemCount)
    {
        var cont = new Container(World.NewItem);
        for (var i = 0; i < itemCount; i++)
        {
            cont.AddItem(new Item(World.NewItem));
        }

        cont.Map = Map.Felucca;

        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();
        m.AccessLevel = AccessLevel.Administrator;
        m.Map = Map.Felucca;

        return (cont, m);
    }

    [Fact]
    public void TestContainerContent()
    {
        var (cont, m) = CreateViewedContainer(1);

        try
        {
            PacketAssert.Sends(new ContainerContent(m, cont), ns => ns.SendContainerContent(m, cont));
        }
        finally
        {
            cont.Delete();
            m.Delete();
        }
    }

    [Fact]
    public void TestContainerContent6017()
    {
        var (cont, m) = CreateViewedContainer(1);

        try
        {
            PacketAssert.Sends(
                new ContainerContent6017(m, cont),
                ns => ns.SendContainerContent(m, cont),
                ProtocolChanges.ContainerGridLines
            );
        }
        finally
        {
            cont.Delete();
            m.Delete();
        }
    }

    // 0x3C's length and count fields are 16-bit: 19-byte entries cap at 3448, 20-byte grid-line entries at 3276.
    [Theory]
    [InlineData(false, 3448, 65517)]
    [InlineData(true, 3276, 65525)]
    public void TestContainerContent_CapsAtProtocolLimit(bool gridLines, int maxEntries, int maxLength)
    {
        // One item past the cap is enough to make truncation engage.
        var (cont, m) = CreateViewedContainer(maxEntries + 1);

        try
        {
            using var ns = PacketTestUtilities.CreateTestNetState();
            if (gridLines)
            {
                ns.ProtocolChanges |= ProtocolChanges.ContainerGridLines;
            }

            // A ~64KB packet exceeds the 4KB pre-auth send buffer; an account lets Send() promote it
            // on demand instead of exhausting the buffer and disconnecting.
            ns.Account = new MockAccount();

            var ex = Record.Exception(() => ns.SendContainerContent(m, cont));
            Assert.Null(ex);

            var span = ns.SendBuffer.GetReadSpan();
            Assert.Equal((byte)0x3C, span[0]);

            var length = BinaryPrimitives.ReadUInt16BigEndian(span[1..3]);
            var count = BinaryPrimitives.ReadUInt16BigEndian(span[3..5]);

            Assert.Equal(maxLength, length);
            Assert.Equal(length, span.Length);
            Assert.Equal(maxEntries, count);
        }
        finally
        {
            cont.Delete();
            m.Delete();
        }
    }

    [Fact]
    public void TestContainerContent_RentedBuffer_MatchesReferencePacket()
    {
        // Below the entry cap but past the 1024-byte stackalloc threshold, so SendContainerContent
        // rents its buffer instead of using the stack.
        var (cont, m) = CreateViewedContainer(100);

        try
        {
            PacketAssert.Sends(new ContainerContent(m, cont), ns => ns.SendContainerContent(m, cont));
        }
        finally
        {
            cont.Delete();
            m.Delete();
        }
    }
}
