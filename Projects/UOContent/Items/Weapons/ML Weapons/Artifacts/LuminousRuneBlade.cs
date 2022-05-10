using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class LuminousRuneBlade : RuneBlade
    {
        [Constructible]
        public LuminousRuneBlade()
        {
            WeaponAttributes.HitLightning = 40;
            WeaponAttributes.SelfRepair = 5;
            Attributes.NightSight = 1;
            Attributes.WeaponSpeed = 25;
            Attributes.WeaponDamage = 55;

            Hue = GetElementalDamageHue();
        }

        public override int LabelNumber => 1072922; // Luminous Rune Blade

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            phys = fire = cold = pois = chaos = direct = 0;
            nrgy = 100;
        }
    }
}
