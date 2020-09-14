using System;
using Server.Engines.ConPVP;

namespace Server.Items
{
    public abstract class BaseSpear : BaseMeleeWeapon
    {
        public BaseSpear(int itemID) : base(itemID)
        {
        }

        public BaseSpear(Serial serial) : base(serial)
        {
        }

        public override int DefHitSound => 0x23C;
        public override int DefMissSound => 0x238;

        public override SkillName DefSkill => SkillName.Fencing;
        public override WeaponType DefType => WeaponType.Piercing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Pierce2H;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);

            if (!Core.AOS && Layer == Layer.TwoHanded &&
                attacker.Skills.Anatomy.Value / 400.0 >= Utility.RandomDouble() &&
                DuelContext.AllowSpecialAbility(attacker, "Paralyzing Blow", false))
            {
                defender.SendMessage("You receive a paralyzing blow!"); // Is this not localized?
                defender.Freeze(TimeSpan.FromSeconds(2.0));

                attacker.SendMessage("You deliver a paralyzing blow!"); // Is this not localized?
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
