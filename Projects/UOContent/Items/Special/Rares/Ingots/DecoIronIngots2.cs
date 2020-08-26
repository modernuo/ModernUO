namespace Server.Items
{
    public class DecoIronIngots2 : Item
    {
        [Constructible]
        public DecoIronIngots2() : base(0x1BF0)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoIronIngots2(Serial serial) : base(serial)
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
