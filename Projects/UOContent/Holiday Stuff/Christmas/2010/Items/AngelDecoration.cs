using ModernUO.Serialization;

namespace Server.Items.Holiday
{
    [SerializationGenerator(0, false)]
    [TypeAlias("Server.Items.AngelDecoration"), Flippable(0x46FA, 0x46FB)]
    public partial class AngelDecoration : Item
    {
        public AngelDecoration() : base(0x46FA)
        {
            LootType = LootType.Blessed;

            Weight = 30;
        }
    }
}
