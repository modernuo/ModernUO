namespace Server.Items
{
    public class DecoGarlic : Item
    {
        [Constructible]
        public DecoGarlic() : base(0x18E1)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoGarlic(Serial serial) : base(serial)
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
