namespace Server.Items
{
    public class DecoRocks : Item
    {
        [Constructible]
        public DecoRocks() : base(0x1367)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoRocks(Serial serial) : base(serial)
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
