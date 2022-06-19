using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class EmeraldMace : DiamondMace
    {
        [Constructible]
        public EmeraldMace() => WeaponAttributes.ResistPoisonBonus = 5;

        public override int LabelNumber => 1073530; // emerald mace
    }
}
