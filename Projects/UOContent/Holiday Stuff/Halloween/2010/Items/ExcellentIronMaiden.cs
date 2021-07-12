namespace Server.Items
{
    [Serializable(0, false)]
    public partial class ExcellentIronMaiden : Item
    {
        [Constructible]
        public ExcellentIronMaiden() : base(0x3f15)
        {
        }

        public override double DefaultWeight => 5;
    }
}
