namespace Server.Items
{
    [Serializable(0, false)]
    public partial class Ginseng : BaseReagent, ICommodity
    {
        [Constructible]
        public Ginseng(int amount = 1) : base(0xF85, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;
    }
}
