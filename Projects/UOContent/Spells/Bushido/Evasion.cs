using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Bushido
{
    public class Evasion : SamuraiSpell
    {
        private static readonly SpellInfo _info = new(
            "Evasion",
            null,
            -1,
            9002
        );

        private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();

        public Evasion(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.25);

        public override double RequiredSkill => 60.0;
        public override int RequiredMana => 10;

        public override bool CheckCast() => VerifyCast(Caster, true) && base.CheckCast();

        public static bool VerifyCast(Mobile caster, bool messages)
        {
            if (caster == null) // Sanity
            {
                return false;
            }

            var weap =
                caster.FindItemOnLayer<BaseWeapon>(Layer.OneHanded) ??
                caster.FindItemOnLayer<BaseWeapon>(Layer.TwoHanded);

            if (weap == null)
            {
                if (caster.FindItemOnLayer(Layer.TwoHanded) is not BaseShield)
                {
                    if (messages)
                    {
                        // You must have a weapon or a shield equipped to use this ability!
                        caster.SendLocalizedMessage(1062944);
                    }

                    return false;
                }
            }
            else
            {
                if (Core.ML && caster.Skills[weap.Skill].Base < 50)
                {
                    if (messages)
                    {
                        // Your skill with your equipped weapon must be 50 or higher to use Evasion.
                        caster.SendLocalizedMessage(1076206);
                    }

                    return false;
                }
            }

            if (!caster.CanBeginAction<Evasion>())
            {
                if (messages)
                {
                    caster.SendLocalizedMessage(501789); // You must wait before trying again.
                }

                return false;
            }

            return true;
        }

        public static bool CheckSpellEvasion(Mobile defender)
        {
            var weap =
                defender.FindItemOnLayer<BaseWeapon>(Layer.OneHanded) ??
                defender.FindItemOnLayer<BaseWeapon>(Layer.TwoHanded);

            if (Core.ML)
            {
                if (defender.Spell?.IsCasting == true)
                {
                    return false;
                }

                if (weap != null)
                {
                    if (defender.Skills[weap.Skill].Base < 50)
                    {
                        return false;
                    }
                }
                else if (defender.FindItemOnLayer(Layer.TwoHanded) is not BaseShield)
                {
                    return false;
                }
            }

            if (IsEvading(defender) && BaseWeapon.CheckParry(defender))
            {
                defender.Emote("*evades*"); // Yes.  Eew.  Blame OSI.
                defender.FixedEffect(0x37B9, 10, 16);
                return true;
            }

            return false;
        }

        public override void OnBeginCast()
        {
            base.OnBeginCast();

            Caster.FixedEffect(0x37C4, 10, 7, 4, 3);
        }

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.SendLocalizedMessage(1063120); // You feel that you might be able to deflect any attack!
                Caster.FixedParticles(0x376A, 1, 20, 0x7F5, 0x960, 3, EffectLayer.Waist);
                Caster.PlaySound(0x51B);

                OnCastSuccessful(Caster);

                BeginEvasion(Caster);

                Caster.BeginAction<Evasion>();
                Timer.StartTimer(TimeSpan.FromSeconds(20.0), Caster.EndAction<Evasion>);
            }

            FinishSequence();
        }

        public static bool IsEvading(Mobile m) => _table.ContainsKey(m);

        public static TimeSpan GetEvadeDuration(Mobile m)
        {
            /* Evasion duration now scales with Bushido skill
             *
             * If the player has higher than GM Bushido, and GM Tactics and Anatomy, they get a 1 second bonus
             * Evasion duration range:
             * o 3-6 seconds w/o tactics/anatomy
             * o 6-7 seconds w/ GM+ Bushido and GM tactics/anatomy
             */

            if (!Core.ML)
            {
                return TimeSpan.FromSeconds(8.0);
            }

            double seconds = 3;

            if (m.Skills.Bushido.Value > 60)
            {
                seconds += (m.Skills.Bushido.Value - 60) / 20;
            }

            // Bushido being HIGHER than 100 for bonus is intended
            if (m.Skills.Anatomy.Value >= 100.0 && m.Skills.Tactics.Value >= 100.0 && m.Skills.Bushido.Value > 100.0)
            {
                seconds++;
            }

            return TimeSpan.FromSeconds((int)seconds);
        }

        public static double GetParryScalar(Mobile m)
        {
            /* Evasion modifier to parry now scales with Bushido skill
             *
             * If the player has higher than GM Bushido, and at least GM Tactics and Anatomy, they get a bonus to their evasion modifier (10% bonus to the evasion modifier to parry NOT 10% to the final parry chance)
             *
             * Bonus modifier to parry range: (these are the ranges for the evasion modifier)
             * o 16-40% bonus w/o tactics/anatomy
             * o 42-50% bonus w/ GM+ bushido and GM tactics/anatomy
             */

            if (!Core.ML)
            {
                return 1.5;
            }

            double bonus = 0;

            if (m.Skills.Bushido.Value >= 60)
            {
                bonus += (m.Skills.Bushido.Value - 60) * .004 + 0.16;
            }

            // Bushido being HIGHER than 100 for bonus is intended
            if (m.Skills.Anatomy.Value >= 100 && m.Skills.Tactics.Value >= 100 && m.Skills.Bushido.Value > 100)
            {
                bonus += 0.10;
            }

            return 1.0 + bonus;
        }

        public static void BeginEvasion(Mobile m)
        {
            StopEvasionTimer(m);

            Timer.StartTimer(GetEvadeDuration(m),
                () =>
                {
                    EndEvasion(m);
                    m.SendLocalizedMessage(1063121); // You no longer feel that you could deflect any attack.
                },
                out var timerToken
            );

            _table[m] = timerToken;
        }

        private static bool StopEvasionTimer(Mobile m)
        {
            if (_table.Remove(m, out var timer))
            {
                timer.Cancel();
                return true;
            }

            return false;
        }

        public static void EndEvasion(Mobile m)
        {
            if (StopEvasionTimer(m))
            {
                OnEffectEnd(m, typeof(Evasion));
            }
        }
    }
}
