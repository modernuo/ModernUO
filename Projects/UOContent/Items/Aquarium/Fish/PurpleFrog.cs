namespace Server.Items
{
    [Serializable(0, false)]
    public partial class PurpleFrog : BaseFish
    {
        [Constructible]
        public PurpleFrog() : base(0x3B0D) => Hue = 0x4FA;

        public override int LabelNumber => 1073823; // A Purple Frog
    }
}
