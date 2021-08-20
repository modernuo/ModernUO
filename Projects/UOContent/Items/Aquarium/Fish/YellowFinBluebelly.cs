namespace Server.Items
{
    [Serializable(0, false)]
    public partial class YellowFinBluebelly : BaseFish
    {
        [Constructible]
        public YellowFinBluebelly() : base(0x3B07)
        {
        }

        public override int LabelNumber => 1073831; // A Yellow Fin Bluebelly
    }
}
