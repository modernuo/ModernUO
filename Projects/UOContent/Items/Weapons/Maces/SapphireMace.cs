using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class SapphireMace : DiamondMace
    {
        [Constructible]
        public SapphireMace() => WeaponAttributes.ResistEnergyBonus = 5;

        public override int LabelNumber => 1073531; // sapphire mace
    }
}
