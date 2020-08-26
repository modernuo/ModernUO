namespace Server.Items
{
    public class DragonsBlood : BaseReagent, ICommodity
    {
        [Constructible]
        public DragonsBlood(int amount = 1)
            : base(0x4077, amount)
        {
        }

        public DragonsBlood(Serial serial)
            : base(serial)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => Core.ML;

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
