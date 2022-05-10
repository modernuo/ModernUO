using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class HeavyOrnateAxe : OrnateAxe
    {
        [Constructible]
        public HeavyOrnateAxe() => Attributes.WeaponDamage = 8;

        public override int LabelNumber => 1073548; // heavy ornate axe
    }
}
