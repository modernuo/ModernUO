using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class GlacialStaff : BlackStaff
    {
        [Constructible]
        public GlacialStaff()
        {
            Hue = 0x480;
            WeaponAttributes.HitHarm = 5 * Utility.RandomMinMax(1, 5);
            WeaponAttributes.MageWeapon = Utility.RandomMinMax(5, 10);

            AosElementDamages[AosElementAttribute.Cold] = 20 + 5 * Utility.RandomMinMax(0, 6);
        }

        // TODO: Pre-AoS stuff
        public override int LabelNumber => 1017413; // Glacial Staff
    }
}
