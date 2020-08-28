namespace Server.Items
{
    public class Shaft : Item, ICommodity
    {
        [Constructible]
        public Shaft(int amount = 1) : base(0x1BD4)
        {
            Stackable = true;
            Amount = amount;
        }

        public Shaft(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;
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
