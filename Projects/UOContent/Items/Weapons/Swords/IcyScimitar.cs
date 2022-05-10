using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class IcyScimitar : RadiantScimitar
    {
        [Constructible]
        public IcyScimitar() => WeaponAttributes.HitHarm = 15;

        public override int LabelNumber => 1073543; // icy scimitar
    }
}
