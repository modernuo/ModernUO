using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class Runesabre : RuneBlade
    {
        [Constructible]
        public Runesabre()
        {
            SkillBonuses.SetValues(0, SkillName.MagicResist, 5.0);
            WeaponAttributes.MageWeapon = -29;
        }

        public override int LabelNumber => 1073537; // runesabre
    }
}
