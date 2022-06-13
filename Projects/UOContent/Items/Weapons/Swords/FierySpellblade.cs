using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class FierySpellblade : ElvenSpellblade
    {
        [Constructible]
        public FierySpellblade() => WeaponAttributes.ResistFireBonus = 5;

        public override int LabelNumber => 1073515; // fiery spellblade
    }
}
