namespace Server.Items
{
    [Serializable(0, false)]
    public partial class DemonSkull : Item
    {
        [Constructible]
        public DemonSkull() : base(0x224e + Utility.Random(4))
        {
        }
    }
}
