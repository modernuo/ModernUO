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

        //Disarm is special. Doesnt need tactics when wresling and tactics need is lower than fighting skill.
        public override bool RequiresTactics(Mobile from) => false;

        public override bool CheckSkills(Mobile from)
        {
            if (!(from.Weapon is BaseWeapon weapon))
            {
                return base.CheckSkills(from);
            }

            var skill = from.Skills.Tactics;
            var skillReq = GetRequiredSecondarySkill(from);

            if (skill?.Value >= skillReq)
            {
                return true;
            }

            //TODO - find correct message for tactics only.
            from.SendLocalizedMessage(1079308, skillReq.ToString()); // You need ~1_SKILL_REQUIREMENT~ weapon and tactics skill to perform that attack

            return false;
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            var toDisarm = defender.FindItemOnLayer(Layer.OneHanded);

            if (toDisarm is null || toDisarm?.Movable == false)
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

                BuffInfo.AddBuff(defender, new BuffInfo(BuffIcon.NoRearm, 1075637, BlockEquipDuration, defender));

                BaseWeapon.BlockEquip(defender, BlockEquipDuration);
            }
        }
    }
}
