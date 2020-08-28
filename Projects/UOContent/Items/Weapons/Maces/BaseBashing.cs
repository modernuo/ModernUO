using Server.Engines.ConPVP;

namespace Server.Items
{
    public abstract class BaseBashing : BaseMeleeWeapon
    {
        public BaseBashing(int itemID) : base(itemID)
        {
        }

        public BaseBashing(Serial serial) : base(serial)
        {
        }

        public override int DefHitSound => 0x233;
        public override int DefMissSound => 0x239;

        public override SkillName DefSkill => SkillName.Macing;
        public override WeaponType DefType => WeaponType.Bashing;
        public override WeaponAnimation DefAnimation => WeaponAnimation.Bash1H;

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

                attacker.SendMessage("You deliver a crushing blow!"); // Is this not localized?
                attacker.PlaySound(0x11C);
            }

            return damage;
        }
    }
}
