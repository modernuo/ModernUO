namespace Server.Items
{
    [Serializable(0, false)]
    public partial class Nightshade : BaseReagent, ICommodity
    {
        [Constructible]
        public Nightshade(int amount = 1) : base(0xF88, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;
    }
}
