using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BrownBook : BaseBook
    {
        [Constructible]
        public BrownBook() : base(0xFEF)
        {
        }

        [Constructible]
        public BrownBook(int pageCount, bool writable) : base(0xFEF, pageCount, writable)
        {
        }

        [Constructible]
        public BrownBook(string title, string author, int pageCount, bool writable) : base(
            0xFEF,
            title,
            author,
            pageCount,
            writable
        )
        {
        }

        // Intended for defined books only
        public BrownBook(bool writable) : base(0xFEF, writable)
        {
        }
    }
}
