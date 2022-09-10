using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PixieSwatter : Scepter
{
    [Constructible]
    public PixieSwatter()
    {
        Hue = 0x8A;
        WeaponAttributes.HitPoisonArea = 75;
        Attributes.WeaponSpeed = 30;

        WeaponAttributes.UseBestSkill = 1;
        WeaponAttributes.ResistFireBonus = 12;
        WeaponAttributes.ResistEnergyBonus = 12;

        Slayer = SlayerName.Fey;
    }

    public override int LabelNumber => 1070854; // Pixie Swatter

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois,
        out int nrgy, out int chaos, out int direct
    )
    {
        fire = 100;
        cold = pois = phys = nrgy = chaos = direct = 0;
    }
}
