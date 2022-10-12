using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ColdBlood : Cleaver
{
    [Constructible]
    public ColdBlood()
    {
        Hue = 0x4F2;

        Attributes.WeaponSpeed = 40;

        Attributes.BonusHits = 6;
        Attributes.BonusStam = 6;
        Attributes.BonusMana = 6;
    }

    public override int LabelNumber => 1070818; // Cold Blood

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        cold = 100;
        fire = phys = pois = nrgy = chaos = direct = 0;
    }
}
