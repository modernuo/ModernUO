namespace Server.Items
{
    public class DecoIronIngots5 : Item
    {
        [Constructible]
        public DecoIronIngots5() : base(0x1BF3)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoIronIngots5(Serial serial) : base(serial)
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
