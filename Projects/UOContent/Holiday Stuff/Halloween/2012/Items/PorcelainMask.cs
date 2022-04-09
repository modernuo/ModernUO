using ModernUO.Serialization;

namespace Server.Items.Holiday
{
    [SerializationGenerator(0, false)]
    public partial class PaintedPorcelainMask : BasePaintedMask
    {
        [Constructible]
        public PaintedPorcelainMask() : base(0x4BA7)
        {
        }

        public override string MaskName => "Porcelain Mask";
    }
}
