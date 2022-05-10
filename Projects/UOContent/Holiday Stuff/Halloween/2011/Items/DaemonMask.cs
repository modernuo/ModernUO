using ModernUO.Serialization;

namespace Server.Items.Holiday
{
    [SerializationGenerator(0, false)]
    public partial class PaintedDaemonMask : BasePaintedMask
    {
        [Constructible]
        public PaintedDaemonMask()
            : base(0x4a92)
        {
        }

        public override string MaskName => "Daemon Mask";
    }
}
