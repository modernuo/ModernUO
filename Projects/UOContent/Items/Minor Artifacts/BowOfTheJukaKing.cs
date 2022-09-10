using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BowOfTheJukaKing : Bow
{
    [Constructible]
    public BowOfTheJukaKing()
    {
        Hue = 0x460;
        WeaponAttributes.HitMagicArrow = 25;
        Slayer = SlayerName.ReptilianDeath;
        Attributes.AttackChance = 15;
        Attributes.WeaponDamage = 40;
    }

    public override int LabelNumber => 1070636;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
}
