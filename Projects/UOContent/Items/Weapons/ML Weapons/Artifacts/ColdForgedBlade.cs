using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ColdForgedBlade : ElvenSpellblade
    {
        [Constructible]
        public ColdForgedBlade()
        {
            WeaponAttributes.HitHarm = 40;
            Attributes.SpellChanneling = 1;
            Attributes.NightSight = 1;
            Attributes.WeaponSpeed = 25;
            Attributes.WeaponDamage = 50;

            Hue = GetElementalDamageHue();
        }

        public override int LabelNumber => 1072916; // Cold Forged Blade

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            phys = fire = pois = nrgy = chaos = direct = 0;
            cold = 100;
        }
    }
}
