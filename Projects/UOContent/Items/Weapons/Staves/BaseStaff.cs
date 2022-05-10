using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseStaff : BaseMeleeWeapon
    {
        public BaseStaff(int itemID) : base(itemID)
        {
        }

        public override int DefHitSound => 0x233;
        public override int DefMissSound => 0x239;

        public override SkillName DefSkill => SkillName.Macing;
        public override WeaponType DefType => WeaponType.Staff;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Bash2H;

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);

            defender.Stam -= Utility.Random(3, 3); // 3-5 points of stamina loss
        }
    }
}
