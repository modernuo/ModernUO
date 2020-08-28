namespace Server.Items
{
    public class ZoogiFungus : Item, ICommodity
    {
        [Constructible]
        public ZoogiFungus(int amount = 1) : base(0x26B7)
        {
            Stackable = true;
            Weight = 0.1;
            Amount = amount;
        }

        public ZoogiFungus(Serial serial) : base(serial)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => Core.ML;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
