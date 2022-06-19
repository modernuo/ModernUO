using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class TrueLeafblade : Leafblade
    {
        [Constructible]
        public TrueLeafblade() => WeaponAttributes.ResistPoisonBonus = 5;

        public override int LabelNumber => 1073521; // true leafblade
    }
}
