namespace Server.Items
{
    public class DecoGoldIngots3 : Item
    {
        [Constructible]
        public DecoGoldIngots3() : base(0x1BED)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoGoldIngots3(Serial serial) : base(serial)
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
