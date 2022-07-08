using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class TanBook : BaseBook
    {
        [Constructible]
        public TanBook() : base(0xFF0)
        {
        }

        [Constructible]
        public TanBook(int pageCount, bool writable) : base(0xFF0, pageCount, writable)
        {
        }

        [Constructible]
        public TanBook(string title, string author, int pageCount, bool writable) : base(
            0xFF0,
            title,
            author,
            pageCount,
            writable
        )
        {
        }

        // Intended for defined books only
        public TanBook(bool writable) : base(0xFF0, writable)
        {
        }
    }
}
