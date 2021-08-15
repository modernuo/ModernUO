namespace Server.Items
{
    [Serializable(0, false)]
    public partial class DaemonBone : BaseReagent, ICommodity
    {
        [Constructible]
        public DaemonBone(int amount = 1) : base(0xF80, amount)
        {
        }

        public override double DefaultWeight => 1.0;

        int ICommodity.DescriptionNumber => LabelNumber;
        bool ICommodity.IsDeedable => Core.ML;
    }
}
