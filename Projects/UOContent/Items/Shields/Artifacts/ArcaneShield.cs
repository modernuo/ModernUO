using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ArcaneShield : WoodenKiteShield
{
    [Constructible]
    public ArcaneShield()
    {
        ItemID = 0x1B78;
        Hue = 0x556;
        Attributes.NightSight = 1;
        Attributes.SpellChanneling = 1;
        Attributes.DefendChance = 15;
        Attributes.CastSpeed = 1;
    }

    public override int LabelNumber => 1061101; // Arcane Shield
    public override int ArtifactRarity => 11;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
