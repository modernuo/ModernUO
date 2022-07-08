using System;
using ModernUO.Serialization;
using Server.Engines.ConPVP;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public abstract partial class BaseSpear : BaseMeleeWeapon
    {
        public BaseSpear(int itemID) : base(itemID)
        {
        }

        public override int DefHitSound => 0x23C;
        public override int DefMissSound => 0x238;

        public override SkillName DefSkill => SkillName.Fencing;
        public override WeaponType DefType => WeaponType.Piercing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce2H;

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);

            if (!Core.AOS && Layer == Layer.TwoHanded &&
                attacker.Skills.Anatomy.Value / 400.0 >= Utility.RandomDouble() &&
                DuelContext.AllowSpecialAbility(attacker, "Paralyzing Blow", false))
            {
                defender.SendLocalizedMessage(1072221); // You have been hit by a paralyzing blow!
                defender.Freeze(TimeSpan.FromSeconds(2.0));

                attacker.SendLocalizedMessage(1060163); // You deliver a paralyzing blow!
                attacker.PlaySound(0x11C);
            }

            if (!Core.AOS && Poison != null && PoisonCharges > 0)
            {
                --PoisonCharges;

                if (Utility.RandomDouble() >= 0.5) // 50% chance to poison
                {
                    defender.ApplyPoison(attacker, Poison);
                }
            }
        }
    }
}
