using System;
using Server;
using Server.Items;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
    public class BookPacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData("🅵🅰🅽🅲🆈 🆃🅴🆇🆃 Author", "🅵🅰🅽🅲🆈 🆃🅴🆇🆃 Title")]
        public void TestBookCover(string author, string title)
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var book = new BlueBook { Author = author, Title = title };

            var expected = new BookHeader(m, book).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendBookCover(m, book);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestBookContent()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var book = new BlueBook { Author = "Some Author", Title = "Some Title" };
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
                "So everyone can ready: 🅵🅰🅽🅲🆈 🆃🅴🆇🆃"
            };

            book.Pages[2].Lines = new[]
            {
                "The end"
            };

            var expected = new BookPageDetails(book).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendBookContent(book);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
