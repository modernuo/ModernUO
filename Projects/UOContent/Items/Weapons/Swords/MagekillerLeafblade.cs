using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class MagekillerLeafblade : Leafblade
    {
        [Constructible]
        public MagekillerLeafblade() => WeaponAttributes.HitLeechMana = 16;

        public override int LabelNumber => 1073523; // maagekiller leafblade
    }
}
