namespace Server.Items
{
    public class DecoGarlicBulb2 : Item
    {
        [Constructible]
        public DecoGarlicBulb2() : base(0x18E4)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoGarlicBulb2(Serial serial) : base(serial)
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
