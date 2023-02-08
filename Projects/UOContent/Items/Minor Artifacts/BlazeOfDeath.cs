using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BlazeOfDeath : Halberd
{
    [Constructible]
    public BlazeOfDeath()
    {
        Hue = 0x501;
        WeaponAttributes.HitFireArea = 50;
        WeaponAttributes.HitFireball = 50;
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 35;
        WeaponAttributes.ResistFireBonus = 10;
        WeaponAttributes.LowerStatReq = 100;
    }

    public override int LabelNumber => 1063486;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        fire = 50;
        phys = 50;

        cold = pois = nrgy = chaos = direct = 0;
    }
}
