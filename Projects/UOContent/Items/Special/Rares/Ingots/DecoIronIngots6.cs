namespace Server.Items
{
    public class DecoIronIngots6 : Item
    {
        [Constructible]
        public DecoIronIngots6() : base(0x1BF4)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoIronIngots6(Serial serial) : base(serial)
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
