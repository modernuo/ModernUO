using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
    public class StrangleSpell : NecromancerSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Strangle",
            "In Bal Nox",
            209,
            9031,
            Reagent.DaemonBlood,
            Reagent.NoxCrystal
        );

        private static readonly Dictionary<Mobile, InternalTimer> _table = new();

        public StrangleSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

        public override double RequiredSkill => 65.0;
        public override int RequiredMana => 29;

        public void Target(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                // SpellHelper.CheckReflect( (int)this.Circle, Caster, ref m );
                // Irrelevent after AoS

                /* Temporarily chokes off the air suply of the target with poisonous fumes.
                 * The target is inflicted with poison damage over time.
                 * The amount of damage dealt each "hit" is based off of the caster's Spirit Speak skill and the Target's current Stamina.
                 * The less Stamina the target has, the more damage is done by Strangle.
                 * Duration of the effect is Spirit Speak skill level / 10 rounds, with a minimum number of 4 rounds.
                 * The first round of damage is dealt after 5 seconds, and every next round after that comes 1 second sooner than the one before, until there is only 1 second between rounds.
                 * The base damage of the effect lies between (Spirit Speak skill level / 10) - 2 and (Spirit Speak skill level / 10) + 1.
                 * Base damage is multiplied by the following formula: (3 - (target's current Stamina / target's maximum Stamina) * 2).
                 * Example:
                 * For a target at full Stamina the damage multiplier is 1,
                 * for a target at 50% Stamina the damage multiplier is 2 and
                 * for a target at 20% Stamina the damage multiplier is 2.6
                 */

                m.Spell?.OnCasterHurt();

                m.PlaySound(0x22F);
                m.FixedParticles(0x36CB, 1, 9, 9911, 67, 5, EffectLayer.Head);
                m.FixedParticles(0x374A, 1, 17, 9502, 1108, 4, (EffectLayer)255);

                if (!_table.TryGetValue(m, out var timer))
                {
                    _table[m] = timer = new InternalTimer(m, Caster);
                    timer.Start();
                }

                HarmfulSpell(m);
            }

            // Calculations for the buff bar
            var spiritlevel = Math.Min(4, Caster.Skills.SpiritSpeak.Value / 10);

            const int minDamage = 4;
            var maxDamage = ((int)spiritlevel + 1) * 3;
            var args = $"{minDamage}\t{maxDamage}";

            var count = (int)spiritlevel;
            var maxCount = count;
            var hitDelay = 5;
            var length = hitDelay;

            while (count > 1)
            {
                --count;
                if (hitDelay > 1)
                {
                    if (maxCount < 5)
                    {
                        --hitDelay;
                    }
                    else
                    {
                        var delay = (int)Math.Ceiling((1.0 + 5 * count) / maxCount);

                        hitDelay = delay <= 5 ? delay : 5;
                    }
                }

                length += hitDelay;
            }

            var t_Duration = TimeSpan.FromSeconds(length);
            BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Strangle, 1075794, 1075795, t_Duration, m, args));

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }

        public static bool RemoveCurse(Mobile m)
        {
            if (!_table.Remove(m, out var timer))
            {
                return false;
            }

            timer.Stop();
            m.SendLocalizedMessage(1061687); // You can breath normally again.
            return true;
        }

        private class InternalTimer : Timer
        {
            private Mobile _from;
            private double _maxBaseDamage;
            private int _maxCount;
            private double _minBaseDamage;
            private Mobile _target;
            private int _count;
            private int _hitDelay;
            private DateTime _nextHit;

            public InternalTimer(Mobile target, Mobile from) : base(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1))
            {

                _target = target;
                _from = from;

                var spiritLevel = from.Skills.SpiritSpeak.Value / 10;

                _minBaseDamage = spiritLevel - 2;
                _maxBaseDamage = spiritLevel + 1;

                _hitDelay = 5;
                _nextHit = Core.Now + TimeSpan.FromSeconds(_hitDelay);

                _maxCount = _count = Math.Min(4, (int)spiritLevel);
            }

            protected override void OnTick()
            {
                if (!_target.Alive)
                {
                    _table.Remove(_target);
                    Stop();
                }

                if (!_target.Alive || Core.Now < _nextHit)
                {
                    return;
                }

                --_count;

                if (_hitDelay > 1)
                {
                    if (_maxCount < 5)
                    {
                        --_hitDelay;
                    }
                    else
                    {
                        var delay = (int)Math.Ceiling((1.0 + 5 * _count) / _maxCount);

                        if (delay <= 5)
                        {
                            _hitDelay = delay;
                        }
                        else
                        {
                            _hitDelay = 5;
                        }
                    }
                }

                if (_count == 0)
                {
                    _target.SendLocalizedMessage(1061687); // You can breath normally again.
                    _table.Remove(_target);
                    Stop();
                }
                else
                {
                    _nextHit = Core.Now + TimeSpan.FromSeconds(_hitDelay);

                    var damage = _minBaseDamage + Utility.RandomDouble() * (_maxBaseDamage - _minBaseDamage);

                    damage *= 3 - (double)_target.Stam / _target.StamMax * 2;

                    if (damage < 1)
                    {
                        damage = 1;
                    }

                    if (!_target.Player)
                    {
                        damage *= 1.75;
                    }

                    AOS.Damage(_target, _from, (int)damage, 0, 0, 0, 100, 0);

                    // OSI: randomly revealed between first and third damage tick, guessing 60% chance
                    if (Utility.RandomDouble() >= 0.60)
                    {
                        _target.RevealingAction();
                    }
                }
            }
        }
    }
}
