namespace Server.Items
{
    [Serializable(0, false)]
    public partial class SkullPole : Item
    {
        [Constructible]
        public SkullPole() : base(0x2204) => Weight = 5;
    }
}
