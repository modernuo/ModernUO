namespace Server.Items
{
    public class WallBlood : Item
    {
        [Constructible]
        public WallBlood()
            : base(Utility.RandomBool() ? 0x1D95 : 0x1D94)
        {
        }

        public WallBlood(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
