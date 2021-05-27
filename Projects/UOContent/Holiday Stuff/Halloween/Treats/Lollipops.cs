namespace Server.Items
{
    [TypeAlias("Server.Items.Lollipop")]
    [Serializable(0, false)]
    public partial class Lollipops : CandyCane
    {
        [Constructible]
        public Lollipops(int amount = 1) : base(0x468D + Utility.Random(3)) => Stackable = true;
    }
}
