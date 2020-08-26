namespace Server.Items
{
    public class Bone : Item, ICommodity
    {
        [Constructible]
        public Bone(int amount = 1) : base(0xf7e)
        {
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public Bone(Serial serial) : base(serial)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;

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
