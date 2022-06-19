using ModernUO.Serialization;

namespace Server.Items.Holiday
{
    [SerializationGenerator(0, false)]
    public partial class PaintedPlagueMask : BasePaintedMask
    {
        [Constructible]
        public PaintedPlagueMask()
            : base(0x4A8E)
        {
        }

        public override string MaskName => "Plague Mask";
    }
}
