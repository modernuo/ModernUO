using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class BlightGrippedLongbow : ElvenCompositeLongbow
    {
        [Constructible]
        public BlightGrippedLongbow()
        {
            Hue = 0x8A4;

            WeaponAttributes.HitPoisonArea = 20;
            Attributes.RegenStam = 3;
            Attributes.NightSight = 1;
            Attributes.WeaponSpeed = 20;
            Attributes.WeaponDamage = 35;
        }

        public override int LabelNumber => 1072907; // Blight Gripped Longbow
    }
}
