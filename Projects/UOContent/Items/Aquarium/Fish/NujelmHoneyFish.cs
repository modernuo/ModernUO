namespace Server.Items
{
    [Serializable(0, false)]
    public partial class NujelmHoneyFish : BaseFish
    {
        [Constructible]
        public NujelmHoneyFish() : base(0x3B06)
        {
        }

        public override int LabelNumber => 1073830; // A Nujel'm Honey Fish
    }
}
