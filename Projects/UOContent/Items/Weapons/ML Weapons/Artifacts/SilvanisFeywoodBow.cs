using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SilvanisFeywoodBow : ElvenCompositeLongbow
    {
        [Constructible]
        public SilvanisFeywoodBow()
        {
            Hue = 0x1A;

            Attributes.SpellChanneling = 1;
            Attributes.AttackChance = 12;
            Attributes.WeaponSpeed = 30;
            Attributes.WeaponDamage = 35;
        }

        public override int LabelNumber => 1072955; // Silvani's Feywood Bow

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
