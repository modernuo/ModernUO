using ModernUO.Serialization;

namespace Server.Items.Holiday
{
    [SerializationGenerator(0, false)]
    [TypeAlias("Server.Items.RockingHorse"), Flippable(0x4214, 0x4215)]
    public partial class RockingHorse : Item
    {
        public RockingHorse() : base(0x4214)
        {
            LootType = LootType.Blessed;

            Weight = 30;
        }
    }
}
