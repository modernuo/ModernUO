namespace Server.Items.Holiday
{
    public class PaintedDaemonMask : BasePaintedMask
    {
        [Constructible]
        public PaintedDaemonMask()
            : base(0x4a92)
        {
        }

        public PaintedDaemonMask(Serial serial)
            : base(serial)
        {
        }

        public override string MaskName => "Daemon Mask";

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
