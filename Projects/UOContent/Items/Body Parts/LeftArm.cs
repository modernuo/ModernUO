namespace Server.Items
{
    public class LeftArm : Item
    {
        [Constructible]
        public LeftArm() : base(0x1DA1)
        {
        }

        public LeftArm(Serial serial) : base(serial)
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
