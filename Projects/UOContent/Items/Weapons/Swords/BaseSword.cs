using ModernUO.Serialization;
using Server.Targets;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseSword : BaseMeleeWeapon
    {
        public BaseSword(int itemID) : base(itemID)
        {
        }

        public override SkillName DefSkill => SkillName.Swords;
        public override WeaponType DefType => WeaponType.Slashing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Slash1H;

        public override void OnDoubleClick(Mobile from)
        {
            from.SendLocalizedMessage(1010018); // What do you want to use this item on?

            from.Target = new BladedItemTarget(this);
        }

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);

            if (!Core.AOS && Poison != null && PoisonCharges > 0)
            {
                --PoisonCharges;

                if (Utility.RandomBool()) // 50% chance to poison
                {
                    defender.ApplyPoison(attacker, Poison);
                }
            }
        }
    }
}
