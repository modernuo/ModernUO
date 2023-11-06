using System;

namespace Server.Items
{
    /// <summary>
    ///     Does damage and paralyses your opponent for a short time.
    /// </summary>
    public class NerveStrike : WeaponAbility
    {
        public override int BaseMana => 30;

        public override bool RequiresSecondarySkill(Mobile from) => true;
        public override SkillName GetSecondarySkillName(Mobile from) => SkillName.Bushido;

        public override bool OnBeforeSwing(Mobile attacker, Mobile defender)
        {
            if (defender.Paralyzed)
            {
                attacker.SendLocalizedMessage(1061923); // The target is already frozen.
                return false;
            }

            return true;
        }

        public override void OnHit(Mobile attacker, Mobile defender, int damage, WorldLocation worldLocation)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentAbility(attacker);

            var cantpara = Items.ParalyzingBlow.IsImmune(defender);

            if (cantpara)
            {
                attacker.SendLocalizedMessage(1070804); // Your target resists paralysis.
                defender.SendLocalizedMessage(1070813); // You resist paralysis.
            }
            else
            {
                attacker.SendLocalizedMessage(1063356); // You cripple your target with a nerve strike!
                defender.SendLocalizedMessage(1063357); // Your attacker dealt a crippling nerve strike!
            }

            attacker.PlaySound(0x204);
            defender.FixedEffect(0x376A, 9, 32);
            defender.FixedParticles(0x37C4, 1, 8, 0x13AF, 0, 0, EffectLayer.Waist);

            if (Core.ML)
            {
                AOS.Damage(
                    defender,
                    attacker,
                    (int)(15.0 * (attacker.Skills.Bushido.Value - 50.0) / 70.0 + Utility.Random(10)),
                    true,
                    100,
                    0,
                    0,
                    0,
                    0
                ); // 0-25

                if (!cantpara && (150.0 / 7.0 + 4.0 * attacker.Skills.Bushido.Value / 7.0) / 100.0 >
                    Utility.RandomDouble())
                {
                    defender.Paralyze(TimeSpan.FromSeconds(2.0));
                    Items.ParalyzingBlow.BeginImmunity(defender, Items.ParalyzingBlow.FreezeDelayDuration);
                }
            }
            else if (!cantpara)
            {
                AOS.Damage(
                    defender,
                    attacker,
                    (int)(15.0 * (attacker.Skills.Bushido.Value - 50.0) / 70.0 + 10),
                    true,
                    100,
                    0,
                    0,
                    0,
                    0
                ); // 10-25
                defender.Freeze(TimeSpan.FromSeconds(2.0));
                Items.ParalyzingBlow.BeginImmunity(defender, Items.ParalyzingBlow.FreezeDelayDuration);
            }
        }
    }
}
