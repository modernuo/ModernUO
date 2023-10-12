using ModernUO.Serialization;

namespace Server.Engines.Quests.Doom;

[SerializationGenerator(0, false)]
public partial class GrandGrimoire : Item
{
    [Constructible]
    public GrandGrimoire() : base(0xEFA)
    {
        Weight = 1.0;
        Hue = 0x835;
        Layer = Layer.OneHanded;
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1060801; // The Grand Grimoire
}
