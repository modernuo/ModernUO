using ModernUO.Serialization;

namespace Server.Engines.Quests.Doom;

[SerializationGenerator(0, false)]
public partial class GrandGrimoire : Item
{
    [Constructible]
    public GrandGrimoire() : base(0xEFA)
    {
        Hue = 0x835;
        Layer = Layer.OneHanded;
        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1060801; // The Grand Grimoire
}
