namespace Server.Items
{
    [Furniture]
    [Flippable(0xB2D, 0xB2C)]
    [Serializable(0, false)]
    public partial class WoodenBench : Item
    {
        [Constructible]
        public WoodenBench() : base(0xB2D) => Weight = 6;
    }
}
