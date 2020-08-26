namespace Server.Items
{
    public class DecoIronIngot : Item
    {
        [Constructible]
        public DecoIronIngot() : base(0x1BEF)
        {
            Movable = true;
            Stackable = true;
        }

        public DecoIronIngot(Serial serial) : base(serial)
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
