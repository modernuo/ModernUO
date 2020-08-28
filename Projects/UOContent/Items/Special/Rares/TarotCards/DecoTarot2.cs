namespace Server.Items
{
    public class DecoTarot2 : Item
    {
        [Constructible]
        public DecoTarot2() : base(0x12A6)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoTarot2(Serial serial) : base(serial)
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
