namespace Server.Items
{
    public class DecoGarlicBulb : Item
    {
        [Constructible]
        public DecoGarlicBulb() : base(0x18E3)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoGarlicBulb(Serial serial) : base(serial)
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
