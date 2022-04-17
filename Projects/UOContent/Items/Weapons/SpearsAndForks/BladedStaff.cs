using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x26BD, 0x26C7)]
    [SerializationGenerator(0, false)]
    public partial class BladedStaff : BaseSpear
    {
        [Constructible]
        public BladedStaff() : base(0x26BD) => Weight = 4.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.ArmorIgnore;
        public override WeaponAbility SecondaryAbility => WeaponAbility.Dismount;

        public override int AosStrengthReq => 40;
        public override int AosMinDamage => 14;
        public override int AosMaxDamage => 16;
        public override int AosSpeed => 37;
        public override float MlSpeed => 3.00f;

        public override int OldStrengthReq => 40;
        public override int OldMinDamage => 14;
        public override int OldMaxDamage => 16;
        public override int OldSpeed => 37;

        public override int InitMinHits => 21;
        public override int InitMaxHits => 110;

        public override SkillName DefSkill => SkillName.Swords;
    }
}
