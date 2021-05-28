namespace Server.Items.Holiday
{
    public class PaintedEvilJesterMask : BasePaintedMask
    {
        [Constructible]
        public PaintedEvilJesterMask()
            : base(0x4BA5)
        {
        }

        public PaintedEvilJesterMask(Serial serial)
            : base(serial)
        {
        }

        public override string MaskName => "Evil Jester Mask";

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
