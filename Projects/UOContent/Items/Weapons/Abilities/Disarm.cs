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

        public override bool RequiresTactics(Mobile from) => from.Weapon is not BaseWeapon { Skill: SkillName.Wrestling };

        // Disarm is special. Doesnt need tactics when wresling and tactics need is lower than fighting skill.
        public virtual double GetRequiredTactics(Mobile from)
        {
            if (from.Weapon is BaseWeapon weapon)
            {
                if (weapon.PrimaryAbility == this)
                {
                    return 30.0;
                }

                if (weapon.SecondaryAbility == this)
                {
                    return 60.0;
                }
            }

            return 200.0;
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            var toDisarm = defender.FindItemOnLayer(Layer.OneHanded);

            if (toDisarm?.Movable != true)
            {
                toDisarm = defender.FindItemOnLayer(Layer.TwoHanded);
            }

            var pack = defender.Backpack;

            if (pack == null || toDisarm?.Movable == false)
            {
                attacker.SendLocalizedMessage(1004001); // You cannot disarm your opponent.
            }
            else if (!Core.ML && toDisarm == null || toDisarm is BaseShield or Spellbook)
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
