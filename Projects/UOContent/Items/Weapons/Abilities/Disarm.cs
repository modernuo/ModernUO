using System;

namespace Server.Items
{
    /// <summary>
    ///     This attack allows you to disarm your foe.
    ///     Now in Age of Shadows, a successful Disarm leaves the victim unable to re-arm another weapon for several seconds.
    /// </summary>
    public class Disarm : WeaponAbility
    {
        public static readonly TimeSpan BlockEquipDuration = TimeSpan.FromSeconds(5.0);

        public override int BaseMana => 20;

        // No longer active in pub21:
        /*public override bool CheckSkills( Mobile from )
        {
          if (!base.CheckSkills( from ))
            return false;

          if (!(from.Weapon is Fists))
            return true;

          Skill skill = from.Skills.ArmsLore;

          if (skill?.Base >= 80.0)
            return true;

          from.SendLocalizedMessage( 1061812 ); // You lack the required skill in armslore to perform that attack!

          return false;
        }*/

        public override bool RequiresTactics(Mobile from)
        {
            if (!(from.Weapon is BaseWeapon weapon))
            {
                return false;
            }

            return weapon.Skill != SkillName.Wrestling;
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            var toDisarm = defender.FindItemOnLayer(Layer.OneHanded);

            if (toDisarm?.Movable == false)
            {
                toDisarm = defender.FindItemOnLayer(Layer.TwoHanded);
            }

            var pack = defender.Backpack;

            if (pack == null || toDisarm?.Movable == false)
            {
                attacker.SendLocalizedMessage(1004001); // You cannot disarm your opponent.
            }
            else if (!Core.ML && toDisarm == null || toDisarm is BaseShield || toDisarm is Spellbook)
            {
                attacker.SendLocalizedMessage(1060849); // Your target is already unarmed!
            }
            else
            {
                attacker.SendLocalizedMessage(1060092); // You disarm their weapon!
                defender.SendLocalizedMessage(1060093); // Your weapon has been disarmed!

                defender.PlaySound(0x3B9);
                defender.FixedParticles(0x37BE, 232, 25, 9948, EffectLayer.LeftHand);

                pack.DropItem(toDisarm);

                BaseWeapon.BlockEquip(defender, BlockEquipDuration);
            }
        }
    }
}
