using System;
using System.Collections.Generic;

namespace Server.Items
{
    /// <summary>
    ///     A successful Paralyzing Blow will leave the target stunned, unable to move, attack, or cast spells, for a few seconds.
    /// </summary>
    public class ParalyzingBlow : WeaponAbility
    {
        public static readonly TimeSpan PlayerFreezeDuration = TimeSpan.FromSeconds(3.0);
        public static readonly TimeSpan NPCFreezeDuration = TimeSpan.FromSeconds(6.0);

        public static readonly TimeSpan FreezeDelayDuration = TimeSpan.FromSeconds(8.0);

        private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();

        public override int BaseMana => 30;

        // When using Wrestling, tactics isn't needed.
        public override bool RequiresTactics(Mobile from) =>
            Core.AOS && from.Weapon is not BaseWeapon { Skill: SkillName.Wrestling };

        public override bool CheckSkills(Mobile from)
        {
            if (!base.CheckSkills(from))
            {
                return false;
            }

            if (Core.AOS || from.Weapon is not Fists)
            {
                return true;
            }

            if (from.Skills[SkillName.Anatomy] is { Value: >= 80.0 })
            {
                return true;
            }

            from.SendLocalizedMessage(1061811); // You lack the required anatomy skill to perform that attack!

            return false;
        }

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

            if (IsImmune(defender)) // Intentionally going after Mana consumption
            {
                attacker.SendLocalizedMessage(1070804); // Your target resists paralysis.
                defender.SendLocalizedMessage(1070813); // You resist paralysis.
                return;
            }

            defender.FixedEffect(0x376A, 9, 32);
            defender.PlaySound(0x204);

            attacker.SendLocalizedMessage(1060163); // You deliver a paralyzing blow!
            defender.SendLocalizedMessage(1060164); // The attack has temporarily paralyzed you!

            var duration = defender.Player ? PlayerFreezeDuration : NPCFreezeDuration;

            // Pub 21: Treat it as paralyze, not as freeze, effect must be removed when damaged.
            if (Core.AOS)
            {
                defender.Paralyze(duration);
            }
            else
            {
                defender.Freeze(duration);
            }

            BeginImmunity(defender, duration + FreezeDelayDuration);
        }

        public static bool IsImmune(Mobile m) => _table.ContainsKey(m);

        public static void BeginImmunity(Mobile m, TimeSpan duration)
        {
            EndImmunity(m);
            Timer.StartTimer(duration, () => EndImmunity(m), out var timerToken);
            _table[m] = timerToken;
        }

        public static void EndImmunity(Mobile m)
        {
            if (_table.Remove(m, out var timerToken))
            {
                timerToken.Cancel();
            }
        }
    }
}
