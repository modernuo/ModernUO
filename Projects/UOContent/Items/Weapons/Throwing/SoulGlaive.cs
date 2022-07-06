using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class SoulGlaive : BaseThrown
{
    [Constructible]
    public SoulGlaive() : base(0x090A)
    {
        Weight = 8.0;
        Layer = Layer.OneHanded;
    }

    public override int MinThrowRange => 8;

    public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
    public override WeaponAbility SecondaryAbility => WeaponAbility.MortalStrike;
    public override int AosStrengthReq => 60;
    public override int AosMinDamage => 16;
    public override int AosMaxDamage => 20;
    public override float MlSpeed => 4.00f;

    public override int InitMinHits => 31;
    public override int InitMaxHits => 65;
}
