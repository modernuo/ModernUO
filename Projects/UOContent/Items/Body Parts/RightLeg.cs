namespace Server.Items
{
    public class RightLeg : Item
    {
        [Constructible]
        public RightLeg() : base(0x1DA4)
        {
        }

        public RightLeg(Serial serial) : base(serial)
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
