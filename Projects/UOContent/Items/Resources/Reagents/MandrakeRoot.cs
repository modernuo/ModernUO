namespace Server.Items
{
    [Serializable(0, false)]
    public partial class MandrakeRoot : BaseReagent, ICommodity
    {
        [Constructible]
        public MandrakeRoot(int amount = 1) : base(0xF86, amount)
        {
        }

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => true;
    }
}
