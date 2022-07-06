using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Boomerang : BaseThrown
{
    [Constructible]
    public Boomerang() : base(0x8FF)
    {
        Weight = 4.0;
        Layer = Layer.OneHanded;
    }

    public override int MinThrowRange => 4;

    public override WeaponAbility PrimaryAbility => WeaponAbility.MysticArc;
    public override WeaponAbility SecondaryAbility => WeaponAbility.ConcussionBlow;
    public override int AosStrengthReq => 25;
    public override int AosMinDamage => 11;
    public override int AosMaxDamage => 15;
    public override float MlSpeed => 2.75f;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 60;
}
