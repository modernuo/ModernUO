using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NoxRangersHeavyCrossbow : HeavyCrossbow
{
    [Constructible]
    public NoxRangersHeavyCrossbow()
    {
        Hue = 0x58C;
        WeaponAttributes.HitLeechStam = 40;
        Attributes.SpellChanneling = 1;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 20;
        WeaponAttributes.ResistPoisonBonus = 10;
    }

    public override int LabelNumber => 1063485;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        pois = phys = 50;
        fire = cold = nrgy = chaos = direct = 0;
    }
}
