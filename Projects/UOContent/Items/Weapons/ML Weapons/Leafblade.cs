using ModernUO.Serialization;

namespace Server.Items
{
    [Flippable(0x2D22, 0x2D2E)]
    [SerializationGenerator(0)]
    public partial class Leafblade : BaseKnife
    {
        [Constructible]
        public Leafblade() : base(0x2D22) => Weight = 8.0;

        public override WeaponAbility PrimaryAbility => WeaponAbility.Feint;
        public override WeaponAbility SecondaryAbility => WeaponAbility.ArmorIgnore;

        public override int AosStrengthReq => 20;
        public override int AosMinDamage => 13;
        public override int AosMaxDamage => 15;
        public override int AosSpeed => 42;
        public override float MlSpeed => 2.75f;

        public override int OldStrengthReq => 20;
        public override int OldMinDamage => 13;
        public override int OldMaxDamage => 15;
        public override int OldSpeed => 42;

        public override int DefMissSound => 0x239;
        public override SkillName DefSkill => SkillName.Fencing;

        public override int InitMinHits => 30; // TODO
        public override int InitMaxHits => 60; // TODO
    }
}
