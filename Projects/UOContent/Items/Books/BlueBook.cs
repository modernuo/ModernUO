namespace Server.Items
{
    public class BlueBook : BaseBook
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

        public BlueBook(Serial serial) : base(serial)
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
