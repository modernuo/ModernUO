namespace Server.Items
{
    public class TanBook : BaseBook
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

        public TanBook(Serial serial) : base(serial)
        {
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }
    }
}
