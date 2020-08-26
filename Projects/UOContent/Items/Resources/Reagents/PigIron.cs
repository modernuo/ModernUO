namespace Server.Items
{
    public class PigIron : BaseReagent, ICommodity
    {
        [Constructible]
        public PigIron(int amount = 1) : base(0xF8A, amount)
        {
        }

        public PigIron(Serial serial) : base(serial)
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
