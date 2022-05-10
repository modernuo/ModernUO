using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class BlueBook : BaseBook
    {
        [Constructible]
        public BlueBook() : base(0xFF2, 40)
        {
        }

        [Constructible]
        public BlueBook(int pageCount, bool writable) : base(0xFF2, pageCount, writable)
        {
        }

        [Constructible]
        public BlueBook(string title, string author, int pageCount, bool writable) : base(
            0xFF2,
            title,
            author,
            pageCount,
            writable
        )
        {
        }

        // Intended for defined books only
        public BlueBook(bool writable) : base(0xFF2, writable)
        {
        }
    }
}
