namespace Server.Items
{
    public class DecoSilverIngots : Item
    {
        [Constructible]
        public DecoSilverIngots() : base(0x1BFA)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoSilverIngots(Serial serial) : base(serial)
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
