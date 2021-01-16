using Server.Items;
using Server.Text;

namespace Server.Network
{
    public sealed class BookPageDetails : Packet
    {
        public BookPageDetails(BaseBook book) : base(0x66)
        {
            EnsureCapacity(256);

            Stream.Write(book.Serial);
            Stream.Write((ushort)book.PagesCount);

            for (var i = 0; i < book.PagesCount; ++i)
            {
                var page = book.Pages[i];

                Stream.Write((ushort)(i + 1));
                Stream.Write((ushort)page.Lines.Length);

                for (var j = 0; j < page.Lines.Length; ++j)
                {
                    var buffer = page.Lines[j].GetBytesUtf8();

                    Stream.Write(buffer, 0, buffer.Length);
                    Stream.Write((byte)0);
                }
            }
        }
    }

    public sealed class BookHeader : Packet
    {
        public BookHeader(Mobile from, BaseBook book) : base(0xD4)
        {
            var title = book.Title ?? "";
            var author = book.Author ?? "";

            var titleBuffer = title.GetBytesUtf8();
            var authorBuffer = author.GetBytesUtf8();

            EnsureCapacity(15 + titleBuffer.Length + authorBuffer.Length);

            Stream.Write(book.Serial);
            Stream.Write(true);
            Stream.Write(book.Writable && from.InRange(book.GetWorldLocation(), 1));
            Stream.Write((ushort)book.PagesCount);

            Stream.Write((ushort)(titleBuffer.Length + 1));
            Stream.Write(titleBuffer, 0, titleBuffer.Length);
            Stream.Write((byte)0); // terminate

            Stream.Write((ushort)(authorBuffer.Length + 1));
            Stream.Write(authorBuffer, 0, authorBuffer.Length);
            Stream.Write((byte)0); // terminate
        }
    }
}
