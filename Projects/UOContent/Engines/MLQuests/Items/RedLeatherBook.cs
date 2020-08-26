namespace Server.Items
{
    public class RedLeatherBook : BlueBook
    {
        [Constructible]
        public RedLeatherBook() => Hue = 0x485;

        public RedLeatherBook(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
