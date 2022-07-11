using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class Cyclone : BaseThrown
{
    [Constructible]
    public Cyclone() : base(0x901)
    {
        Weight = 6.0;
        Layer = Layer.OneHanded;
    }

    public override int MinThrowRange => 6;

    public override WeaponAbility PrimaryAbility => WeaponAbility.MovingShot;
    public override WeaponAbility SecondaryAbility => WeaponAbility.InfusedThrow;
    public override int AosStrengthReq => 40;
    public override int AosMinDamage => 13;
    public override int AosMaxDamage => 17;
    public override float MlSpeed => 3.25f;
    public override int InitMinHits => 31;
    public override int InitMaxHits => 60;


}
