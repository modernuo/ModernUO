using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ArcticDeathDealer : WarMace
{
    [Constructible]
    public ArcticDeathDealer()
    {
        Hue = 0x480;
        WeaponAttributes.HitHarm = 33;
        WeaponAttributes.HitLowerAttack = 40;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 40;
        WeaponAttributes.ResistColdBonus = 10;
    }

    public override int LabelNumber => 1063481;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        cold = 50;
        phys = 50;

        pois = fire = nrgy = chaos = direct = 0;
    }
}
