using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
    public class MindRotSpell : NecromancerSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Mind Rot",
            "Wis An Ben",
            203,
            9031,
            Reagent.BatWing,
            Reagent.PigIron,
            Reagent.DaemonBlood
        );

        private static readonly Dictionary<Mobile, MRExpireTimer> _table = new();

        public MindRotSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 30.0;
        public override int RequiredMana => 17;

        public void Target(Mobile m)
        {
            if (m == null)
            {
                Caster.SendLocalizedMessage(1060508); // You can't curse that.
            }
            else if (HasMindRotScalar(m))
            {
                Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
            }
            else if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                /* Attempts to place a curse on the Target that increases the mana cost of any spells they cast,
                 * for a duration based off a comparison between the Caster's Spirit Speak skill and the Target's Resisting Spells skill.
                 * The effect lasts for ((Spirit Speak skill level - target's Resist Magic skill level) / 50 ) + 20 seconds.
                 */

                m.Spell?.OnCasterHurt();

                m.PlaySound(0x1FB);
                m.PlaySound(0x258);
                m.FixedParticles(0x373A, 1, 17, 9903, 15, 4, EffectLayer.Head);

                var duration = ((GetDamageSkill(Caster) - GetResistSkill(m)) / 5.0 + 20.0) * (m.Player ? 1.0 : 2.0);
                m.CheckSkill(SkillName.MagicResist, 0.0, 120.0); // Skill check for gain

                SetMindRotScalar(Caster, m, m.Player ? 1.25 : 2.00, TimeSpan.FromSeconds(duration));

                HarmfulSpell(m);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }

        public static bool ClearMindRotScalar(Mobile m)
        {
            if (_table.Remove(m, out var timer))
            {
                timer.Stop();
                m.SendLocalizedMessage(1060872); // Your mind feels normal again.
                BuffInfo.RemoveBuff(m, BuffIcon.Mindrot);

                return true;
            }

            return false;
        }

        public static bool HasMindRotScalar(Mobile m) => _table.ContainsKey(m);

        public static bool GetMindRotScalar(Mobile m, ref double scalar)
        {
            if (_table.TryGetValue(m, out var timer))
            {
                scalar = timer._double;
                return true;
            }

            return false;
        }

        public static void SetMindRotScalar(Mobile caster, Mobile target, double scalar, TimeSpan duration)
        {
            if (!_table.ContainsKey(target))
            {
                var timer = new MRExpireTimer(target, scalar, duration);
                timer.Start();
                _table[target] = timer;

                BuffInfo.AddBuff(target, new BuffInfo(BuffIcon.Mindrot, 1075665, duration, target));
                target.SendLocalizedMessage(1074384);
            }
        }
    }

    public class MRExpireTimer : Timer
    {
        private DateTime _end;
        private Mobile _target;
        public double _double;

        public MRExpireTimer(Mobile target, double scalar, TimeSpan delay) : base(
            TimeSpan.FromSeconds(1.0),
            TimeSpan.FromSeconds(1.0)
        )
        {
            _double = scalar;
            _target = target;
            _end = Core.Now + delay;
        }

        protected override void OnTick()
        {
            if (_target.Deleted || !_target.Alive || Core.Now >= _end)
            {
                MindRotSpell.ClearMindRotScalar(_target);
                Stop();
            }
        }
    }
}
