using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class RubyMace : DiamondMace
    {
        [Constructible]
        public RubyMace() => Attributes.WeaponDamage = 5;

        public override int LabelNumber => 1073529; // ruby mace
    }
}
