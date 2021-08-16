namespace Server.Items
{
    [Serializable(0, false)]
    public partial class FandancerFish : BaseFish
    {
        [Constructible]
        public FandancerFish() : base(0x3B02)
        {
        }

        public override int LabelNumber => 1074591; // Fandancer Fish
    }
}
