using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class CorruptedRuneBlade : RuneBlade
    {
        [Constructible]
        public CorruptedRuneBlade()
        {
            WeaponAttributes.ResistPhysicalBonus = -5;
            WeaponAttributes.ResistPoisonBonus = 12;
        }

        public override int LabelNumber => 1073540; // Corrupted Rune Blade
    }
}
