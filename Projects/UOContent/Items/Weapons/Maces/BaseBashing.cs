using ModernUO.Serialization;
using Server.Engines.ConPVP;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseBashing : BaseMeleeWeapon
    {
        public BaseBashing(int itemID) : base(itemID)
        {
        }

        public override int DefHitSound => 0x233;
        public override int DefMissSound => 0x239;

        public override SkillName DefSkill => SkillName.Macing;
        public override WeaponType DefType => WeaponType.Bashing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Bash1H;

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);

            defender.Stam -= Utility.Random(3, 3); // 3-5 points of stamina loss
        }

        public override double GetBaseDamage(Mobile attacker)
        {
            var damage = base.GetBaseDamage(attacker);

            if (!Core.AOS && (attacker.Player || attacker.Body.IsHuman) && Layer == Layer.TwoHanded &&
                attacker.Skills.Anatomy.Value >= 80 &&
                attacker.Skills.Anatomy.Value / 400.0 >= Utility.RandomDouble() &&
                DuelContext.AllowSpecialAbility(attacker, "Crushing Blow", false))
            {
                damage *= 1.5;

                attacker.SendLocalizedMessage(1060090); // You have delivered a crushing blow!
                attacker.PlaySound(0x11C);
            }

            return damage;
        }
    }
}
