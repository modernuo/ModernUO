namespace Server.Items
{
    public class DeadWood : BaseReagent, ICommodity
    {
        [Constructible]
        public DeadWood(int amount = 1) : base(0xF90, amount)
        {
        }

        public DeadWood(Serial serial) : base(serial)
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
