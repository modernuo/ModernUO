using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class FaerieFire : ElvenCompositeLongbow
    {
        [Constructible]
        public FaerieFire()
        {
            Hue = 0x489;
            WeaponAttributes.HitFireball = 25;
            Balanced = true;
            Attributes.BonusDex = 3;
            Attributes.WeaponSpeed = 20;
            Attributes.WeaponDamage = 60;
        }

        public override int LabelNumber => 1072908; // Faerie Fire

        public override void GetDamageTypes(
            Mobile wielder, out int phys, out int fire, out int cold, out int pois,
            out int nrgy, out int chaos, out int direct
        )
        {
            fire = 100;

            phys = cold = pois = nrgy = chaos = direct = 0;
        }
    }
}
