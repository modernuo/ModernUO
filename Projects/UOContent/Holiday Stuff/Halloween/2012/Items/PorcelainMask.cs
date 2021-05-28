namespace Server.Items.Holiday
{
    public class PaintedPorcelainMask : BasePaintedMask
    {
        [Constructible]
        public PaintedPorcelainMask()
            : base(0x4BA7)
        {
        }

        public PaintedPorcelainMask(Serial serial)
            : base(serial)
        {
        }

        public override string MaskName => "Porcelain Mask";

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
