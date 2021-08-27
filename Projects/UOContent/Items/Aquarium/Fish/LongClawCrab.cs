namespace Server.Items
{
    [Serializable(0, false)]
    public partial class LongClawCrab : BaseFish
    {
        [Constructible]
        public LongClawCrab() : base(0x3AFC) => Hue = 0x527;

        public override int LabelNumber => 1073827; // A Long Claw Crab
    }
}
