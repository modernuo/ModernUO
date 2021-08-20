namespace Server.Items
{
    [Serializable(0, false)]
    public partial class SpeckledCrab : BaseFish
    {
        [Constructible]
        public SpeckledCrab() : base(0x3AFC)
        {
        }

        public override int LabelNumber => 1073826; // A Speckled Crab
    }
}
