namespace Server.Items
{
    public class DecoSilverIngot2 : Item
    {
        [Constructible]
        public DecoSilverIngot2() : base(0x1BF8)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoSilverIngot2(Serial serial) : base(serial)
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
