using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class WildfireBow : ElvenCompositeLongbow
{
    [Constructible]
    public WildfireBow()
    {
        Hue = 0x489;

        SkillBonuses.SetValues(0, SkillName.Archery, 10);
        WeaponAttributes.ResistFireBonus = 25;

        Velocity = 15;
    }

    public override int LabelNumber => 1075044; // Wildfire Bow

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        phys = cold = pois = nrgy = chaos = direct = 0;
        fire = 100;
    }
}
