using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Necromancy;

public class StrangleSpell : NecromancerSpell, ITargetingSpell<Mobile>
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
            // Irrelevant after AoS

            /* Temporarily chokes off the air supply of the target with poisonous fumes.
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

            // According to testing on OSI, it is refreshed.
            if (_table.Remove(m, out var timer))
            {
                timer.Stop();
            }

            timer = new InternalTimer(m, Caster);
            _table[m] = timer;
            timer.Start();

            HarmfulSpell(m);

            /* Note: OSI has a bug where the last "tick" is at 0 seconds and never happens.
             * Example: 100SS -> 39s, 9 ticks, buff icon disappears with 3s left.
             * 5.5, 5, 5, 5, 4, 3.5, 3, 3, 2 -> off.
             *
             * On ModernUO: 100SS -> 34s, 10 ticks
             * We opt for the last tick at 1 second, so the duration is 1 second longer than it needs to be.
             */

            // Calculations for the buff bar
            var power = Math.Max(4, Caster.Skills.SpiritSpeak.Value / 10);

            // Closed formula based on loop:
            // MaxCount = 4 -> 5 + (4 + 3 + 2 + 1)
            // MaxCount > 4 -> 5 + (for each count, Ceiling((1.0 + 5 * count) / power) not to exceed 5)
            var totalLength = power % 5 == 0
                ? 3 * power + 5
                : 3 * power + 3;

            var duration = TimeSpan.FromSeconds(totalLength);

            const int minDamage = 4;
            var maxDamage = ((int)power + 1) * 3;
            var args = $"{minDamage}\t{maxDamage}";

            (m as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.Strangle, 1075794, 1075795, duration, args));
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
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

            var power = from.Skills.SpiritSpeak.Value / 10;

            _minBaseDamage = power - 2;
            _maxBaseDamage = power + 1;

            _hitDelay = 5;
            _nextHit = Core.Now + TimeSpan.FromSeconds(_hitDelay);

            _maxCount = _count = Math.Max(4, (int)power);
        }

        protected override void OnTick()
        {
            if (!_target.Alive)
            {
                _table.Remove(_target);
                Stop();
                return;
            }

            if (Core.Now < _nextHit)
            {
                return;
            }

            if (--_count == 0)
            {
                _target.SendLocalizedMessage(1061687); // You can breathe normally again.
                _table.Remove(_target);
                Stop();
                return;
            }

            if (_hitDelay > 1)
            {
                if (_maxCount < 5)
                {
                    --_hitDelay;
                }
                else
                {
                    _hitDelay = (int)Math.Min(Math.Ceiling((1.0 + 5 * _count) / _maxCount), 5);
                }
            }

            _nextHit = Core.Now + TimeSpan.FromSeconds(_hitDelay);

            var damage = Utility.RandomMinMax(_minBaseDamage, _maxBaseDamage);

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
            if (Utility.RandomDouble() < 0.40)
            {
                _target.RevealingAction();
            }
        }
    }
}
