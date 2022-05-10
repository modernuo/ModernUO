using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class IcySpellblade : ElvenSpellblade
    {
        [Constructible]
        public IcySpellblade() => WeaponAttributes.ResistColdBonus = 5;

        public override int LabelNumber => 1073514; // icy spellblade
    }
}
