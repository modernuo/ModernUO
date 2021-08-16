namespace Server.Items
{
    [Serializable(0, false)]
    public partial class Jellyfish : BaseFish
    {
        [Constructible]
        public Jellyfish() : base(0x3B0E)
        {
        }

        public override int LabelNumber => 1074593; // Jellyfish
    }
}
