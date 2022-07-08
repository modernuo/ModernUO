using ModernUO.Serialization;

namespace Server.Items
{

    [SerializationGenerator(0)]
    public partial class AncientWildStaff : WildStaff
    {
        [Constructible]
        public AncientWildStaff() => WeaponAttributes.ResistPoisonBonus = 5;

        public override int LabelNumber => 1073550; // ancient wild staff
    }
}
