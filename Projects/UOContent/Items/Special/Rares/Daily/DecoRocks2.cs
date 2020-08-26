namespace Server.Items
{
    public class DecoRocks2 : Item
    {
        [Constructible]
        public DecoRocks2() : base(0x136D)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoRocks2(Serial serial) : base(serial)
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
