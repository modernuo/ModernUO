namespace Server.Items
{
    [Serializable(0, false)]
    public partial class Shrimp : BaseFish
    {
        [Constructible]
        public Shrimp() : base(0x3B14)
        {
        }

        public override int LabelNumber => 1074596; // Shrimp
    }
}
