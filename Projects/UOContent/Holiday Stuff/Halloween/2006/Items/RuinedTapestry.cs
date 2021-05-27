namespace Server.Items
{
    [Serializable(0, false)]
    public partial class RuinedTapestry : Item
    {
        [Constructible]
        public RuinedTapestry()
            : base(Utility.RandomBool() ? 0x4699 : 0x469A)
        {
        }

        public override string DefaultName => "Ruined Tapestry ";
    }
}
