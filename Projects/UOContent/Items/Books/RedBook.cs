namespace Server.Items
{
    public class RedBook : BaseBook
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

        public RedBook(Serial serial) : base(serial)
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
