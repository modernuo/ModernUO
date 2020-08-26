namespace Server.Items
{
    public class DecoDragonsBlood : Item
    {
        [Constructible]
        public DecoDragonsBlood() : base(0x4077)
        {
            Movable = true;
            Stackable = false;
        }

        public DecoDragonsBlood(Serial serial) : base(serial)
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
