namespace Server.Items
{
    public class DecoGoldIngot : Item
    {
        [Constructible]
        public DecoGoldIngot() : base(0x1BE9)
        {
            Movable = true;
            Stackable = true;
        }

        public DecoGoldIngot(Serial serial) : base(serial)
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
