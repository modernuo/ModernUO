namespace Server.Items
{
    public class DecoDeckOfTarot : Item
    {
        [Constructible]
        public DecoDeckOfTarot() : base(0x12AB)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoDeckOfTarot(Serial serial) : base(serial)
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
