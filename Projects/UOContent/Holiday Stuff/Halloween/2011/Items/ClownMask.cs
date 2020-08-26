namespace Server.Items.Holiday
{
    public class PaintedEvilClownMask : BasePaintedMask
    {
        [Constructible]
        public PaintedEvilClownMask()
            : base(0x4a90)
        {
        }

        public PaintedEvilClownMask(Serial serial)
            : base(serial)
        {
        }

        public override string MaskName => "Evil Clown Mask";

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
