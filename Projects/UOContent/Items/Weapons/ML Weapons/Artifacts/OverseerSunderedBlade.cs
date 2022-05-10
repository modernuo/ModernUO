using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class OverseerSunderedBlade : RadiantScimitar
    {
        [Constructible]
        public OverseerSunderedBlade()
        {
            ItemID = 0x2D27;
            Hue = 0x485;

            Attributes.RegenStam = 2;
            Attributes.AttackChance = 10;
            Attributes.WeaponSpeed = 35;
            Attributes.WeaponDamage = 45;

            Hue = GetElementalDamageHue();
        }

        public override int LabelNumber => 1072920; // Overseer Sundered Blade

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            phys = cold = pois = nrgy = chaos = direct = 0;
            fire = 100;
        }
    }
}
