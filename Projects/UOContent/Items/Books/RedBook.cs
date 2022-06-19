using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class RedBook : BaseBook
    {
        [Constructible]
        public RedBook() : base(0xFF1)
        {
        }

        [Constructible]
        public RedBook(int pageCount, bool writable) : base(0xFF1, pageCount, writable)
        {
        }

        [Constructible]
        public RedBook(string title, string author, int pageCount, bool writable) : base(
            0xFF1,
            title,
            author,
            pageCount,
            writable
        )
        {
        }

        // Intended for defined books only
        public RedBook(bool writable) : base(0xFF1, writable)
        {
        }
    }
}
