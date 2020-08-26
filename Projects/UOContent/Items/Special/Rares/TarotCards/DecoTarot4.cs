namespace Server.Items
{
    public class DecoTarot4 : Item
    {
        [Constructible]
        public DecoTarot4() : base(0x12A8)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoTarot4(Serial serial) : base(serial)
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
