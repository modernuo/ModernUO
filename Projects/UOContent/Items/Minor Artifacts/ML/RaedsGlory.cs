using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class RaedsGlory : WarCleaver
{
    [Constructible]
    public RaedsGlory()
    {
        ItemID = 0x2D23;
        Hue = 0x1E6;

        Attributes.BonusMana = 8;
        Attributes.SpellChanneling = 1;
        Attributes.WeaponSpeed = 20;

        WeaponAttributes.HitLeechHits = 40;
    }

    public override int LabelNumber => 1075036; // Raed's Glory

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
