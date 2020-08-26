namespace Server.Items
{
    public class DecoGoldIngots4 : Item
    {
        [Constructible]
        public DecoGoldIngots4() : base(0x1BEE)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoGoldIngots4(Serial serial) : base(serial)
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
