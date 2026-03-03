using Server;
using Server.Items;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

public class TestBook : BaseBook
{
    public TestBook(int itemID, int pageCount = 20, bool writable = true) : base(itemID, pageCount, writable)
    {
    }

    public TestBook(int itemID, string title, string author, int pageCount, bool writable) : base(itemID, title, author, pageCount, writable)
    {
    }

    public TestBook(int itemID, bool writable) : base(itemID, writable)
    {
    }

    public TestBook(Serial serial) : base(serial)
    {
        Pages = new BookPageInfo[20];

        for (var i = 0; i < Pages.Length; ++i)
        {
            Pages[i] = new BookPageInfo();
        }
    }
}

[Collection("Sequential UOContent Tests")]
public class BookPacketTests
{
    [Theory]
    [InlineData("🅵🅰🅽🅲🆈 🆃🅴🆇🆃 Author", "🅵🅰🅽🅲🆈 🆃🅴🆇🆃 Title")]
    public void TestBookCover(string author, string title)
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var serial = (Serial)0x1001;
        var book = new TestBook(serial) { Author = author, Title = title };

        var expected = new BookHeader(m, book).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendBookCover(m, book);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestBookContent()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var serial = (Serial)0x1001;
        var book = new TestBook(serial) { Author = "Some Author", Title = "Some Title" };
        book.Pages[0].Lines = new[]
        {
            "Some books start with actual content",
            "This book does not have any actual content",
            "Instead it has several pages of useless text"
        };

        book.Pages[1].Lines = new[]
        {
            "Another page exists but this page:",
            "Has lots of: 🅵🅰🅽🅲🆈 🆃🅴🆇🆃",
            "And just more: 🅵🅰🅽🅲🆈 🆃🅴🆇🆃",
            "So everyone can read: 🅵🅰🅽🅲🆈 🆃🅴🆇🆃"
        };

        book.Pages[2].Lines = new[]
        {
            "The end"
        };

        var expected = new BookPageDetails(book).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendBookContent(book);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
