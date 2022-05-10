using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class DarkglowScimitar : RadiantScimitar
    {
        [Constructible]
        public DarkglowScimitar() => WeaponAttributes.HitDispel = 10;

        public override int LabelNumber => 1073542; // darkglow scimitar
    }
}
