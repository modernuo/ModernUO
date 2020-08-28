namespace Server.Items
{
    public class BrownBook : BaseBook
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

        public BrownBook(Serial serial) : base(serial)
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
