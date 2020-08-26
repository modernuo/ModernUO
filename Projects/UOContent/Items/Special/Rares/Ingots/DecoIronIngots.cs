namespace Server.Items
{
    public class DecoIronIngots : Item
    {
        [Constructible]
        public DecoIronIngots() : base(0x1BF1)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoIronIngots(Serial serial) : base(serial)
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
