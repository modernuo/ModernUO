namespace Server.Items
{
    public class IcyHeart : Item
    {
        [Constructible]
        public IcyHeart() : base(0x24B)
        {
        }

        public IcyHeart(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073162; // Icy Heart

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
