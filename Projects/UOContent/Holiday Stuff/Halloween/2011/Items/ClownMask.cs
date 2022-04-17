using ModernUO.Serialization;

namespace Server.Items.Holiday
{
    [SerializationGenerator(0, false)]
    public partial class PaintedEvilClownMask : BasePaintedMask
    {
        [Constructible]
        public PaintedEvilClownMask() : base(0x4a90)
        {
        }

        public override string MaskName => "Evil Clown Mask";
    }
}
