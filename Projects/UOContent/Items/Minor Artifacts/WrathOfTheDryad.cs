using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WrathOfTheDryad : GnarledStaff
{
    [Constructible]
    public WrathOfTheDryad()
    {
        Hue = 0x29C;
        WeaponAttributes.HitLeechMana = 50;
        WeaponAttributes.HitLightning = 33;
        Attributes.AttackChance = 15;
        Attributes.WeaponDamage = 40;
    }

    public override int LabelNumber => 1070853; // Wrath of the Dryad

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        pois = 100;
        cold = fire = phys = nrgy = chaos = direct = 0;
    }
}
