namespace Server.Items.Holiday
{
    [Serializable(0, false)]
    public partial class PaintedEvilJesterMask : BasePaintedMask
    {
        [Constructible]
        public PaintedEvilJesterMask() : base(0x4BA5)
        {
        }

        public override string MaskName => "Evil Jester Mask";
    }
}
