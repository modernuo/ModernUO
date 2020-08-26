namespace Server.Items
{
    public class DecoDeckOfTarot2 : Item
    {
        [Constructible]
        public DecoDeckOfTarot2() : base(0x12Ac)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoDeckOfTarot2(Serial serial) : base(serial)
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
