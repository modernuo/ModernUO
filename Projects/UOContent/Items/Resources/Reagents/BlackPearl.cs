namespace Server.Items
{
    public class BlackPearl : BaseReagent, ICommodity
    {
        [Constructible]
        public BlackPearl(int amount = 1) : base(0xF7A, amount)
        {
        }

        public BlackPearl(Serial serial) : base(serial)
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
