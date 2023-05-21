using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class HallowedSpellbook : Spellbook
{
    [Constructible]
    public HallowedSpellbook() : base(0x3FFFFFFFF)
    {
        LootType = LootType.Blessed;

        Slayer = SlayerName.Silver;
    }

    public override int LabelNumber => 1077620; // Hallowed Spellbook
}
