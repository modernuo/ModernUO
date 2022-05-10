using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class FrozenLongbow : ElvenCompositeLongbow
    {
        [Constructible]
        public FrozenLongbow()
        {
            Attributes.WeaponSpeed = -5;
            Attributes.DefendChance = 10;
        }

        public override int LabelNumber => 1073507; // frozen longbow
    }
}
