using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class LongbowOfMight : ElvenCompositeLongbow
    {
        [Constructible]
        public LongbowOfMight() => Attributes.WeaponDamage = 5;

        public override int LabelNumber => 1073508; // longbow of might
    }
}
