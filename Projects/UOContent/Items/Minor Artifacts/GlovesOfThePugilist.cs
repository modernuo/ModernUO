using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GlovesOfThePugilist : LeatherGloves
{
    [Constructible]
    public GlovesOfThePugilist()
    {
        Hue = 0x6D1;
        SkillBonuses.SetValues(0, SkillName.Wrestling, 10.0);
        Attributes.BonusDex = 8;
        Attributes.WeaponDamage = 15;
    }

    public override int LabelNumber => 1070690;

    public override int BasePhysicalResistance => 18;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
