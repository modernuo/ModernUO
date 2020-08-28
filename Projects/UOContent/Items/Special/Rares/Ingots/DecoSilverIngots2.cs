namespace Server.Items
{
    public class DecoSilverIngots2 : Item
    {
        [Constructible]
        public DecoSilverIngots2() : base(0x1BF6)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoSilverIngots2(Serial serial) : base(serial)
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
