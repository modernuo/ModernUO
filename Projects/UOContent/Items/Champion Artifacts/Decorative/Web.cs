namespace Server.Items
{
    [Serializable(0, false)]
    public partial class Web : Item
    {
        private static readonly int[] ItemIds = { 0x10d7, 0x10d8, 0x10dd };

        [Constructible]
        public Web() : base(ItemIds[Utility.Random(3)])
        {
        }

        [Constructible]
        public Web(int itemid) : base(itemid)
        {
        }
    }
}
