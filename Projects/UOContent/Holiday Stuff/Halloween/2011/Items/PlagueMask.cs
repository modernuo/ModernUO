namespace Server.Items.Holiday
{
    public class PaintedPlagueMask : BasePaintedMask
    {
        [Constructible]
        public PaintedPlagueMask()
            : base(0x4A8E)
        {
        }

        public PaintedPlagueMask(Serial serial)
            : base(serial)
        {
        }

        public override string MaskName => "Plague Mask";

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
