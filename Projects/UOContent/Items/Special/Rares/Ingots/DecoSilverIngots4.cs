namespace Server.Items
{
    public class DecoSilverIngots4 : Item
    {
        [Constructible]
        public DecoSilverIngots4() : base(0x1BF9)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoSilverIngots4(Serial serial) : base(serial)
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
