namespace Server.Items
{
    public class Nightshade : BaseReagent, ICommodity
    {
        [Constructible]
        public Nightshade(int amount = 1) : base(0xF88, amount)
        {
        }

        public Nightshade(Serial serial) : base(serial)
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
