namespace Server.Items
{
    public class DecoMandrakeRoot : Item
    {
        [Constructible]
        public DecoMandrakeRoot() : base(0x18DE)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoMandrakeRoot(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
