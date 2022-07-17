using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
    public class BloodOathSpell : NecromancerSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Blood Oath",
            "In Jux Mani Xen",
            203,
            9031,
            Reagent.DaemonBlood
        );

        private static readonly Dictionary<Mobile, Mobile> _oathTable = new();
        private static readonly Dictionary<Mobile, ExpireTimer> _table = new();

        public BloodOathSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 20.0;
        public override int RequiredMana => 13;

        public void Target(Mobile m)
        {
            if (m == null)
            {
                Caster.SendLocalizedMessage(1060508); // You can't curse that.
            }
            // only PlayerMobile and BaseCreature implement blood oath checking
            else if (Caster == m || m is not (PlayerMobile or BaseCreature))
            {
                Caster.SendLocalizedMessage(1060508); // You can't curse that.
            }
            else if (_oathTable.ContainsKey(Caster))
            {
                Caster.SendLocalizedMessage(1061607); // You are already bonded in a Blood Oath.
            }
            else if (_oathTable.ContainsKey(m))
            {
                if (m.Player)
                {
                    Caster.SendLocalizedMessage(1061608); // That player is already bonded in a Blood Oath.
                }
                else
                {
                    Caster.SendLocalizedMessage(1061609); // That creature is already bonded in a Blood Oath.
                }
            }
            else if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                /* Temporarily creates a dark pact between the caster and the target.
                 * Any damage dealt by the target to the caster is increased, but the target receives the same amount of damage.
                 * The effect lasts for ((Spirit Speak skill level - target's Resist Magic skill level) / 80 ) + 8 seconds.
                 *
                 * NOTE: The above algorithm must be fixed point, it should be:
                 * ((ss-rm)/8)+8
                 */

                RemoveCurse(m);

                _oathTable[Caster] = Caster;
                _oathTable[m] = Caster;

                m.Spell?.OnCasterHurt();

                Caster.PlaySound(0x175);

                Caster.FixedParticles(0x375A, 1, 17, 9919, 33, 7, EffectLayer.Waist);
                Caster.FixedParticles(0x3728, 1, 13, 9502, 33, 7, (EffectLayer)255);

                m.FixedParticles(0x375A, 1, 17, 9919, 33, 7, EffectLayer.Waist);
                m.FixedParticles(0x3728, 1, 13, 9502, 33, 7, (EffectLayer)255);

                var duration = TimeSpan.FromSeconds((GetDamageSkill(Caster) - GetResistSkill(m)) / 8 + 8);
                m.CheckSkill(SkillName.MagicResist, 0.0, 120.0); // Skill check for gain

                var timer = new ExpireTimer(Caster, m, duration);
                timer.Start();

                BuffInfo.AddBuff(Caster, new BuffInfo(BuffIcon.BloodOathCaster, 1075659, duration, Caster, m.Name));
                BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.BloodOathCurse, 1075661, duration, m, Caster.Name));

                _table[m] = timer;
                HarmfulSpell(m);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }

        public static bool RemoveCurse(Mobile target)
        {
            if (_table.Remove(target, out var timer))
            {
                var caster = timer.Caster;
                if (_oathTable.Remove(caster))
                {
                    caster.SendLocalizedMessage(1061620); // Your Blood Oath has been broken.
                }

                if (_oathTable.Remove(target))
                {
                    target.SendLocalizedMessage(1061620); // Your Blood Oath has been broken.
                }

                timer.Stop();

                BuffInfo.RemoveBuff(caster, BuffIcon.BloodOathCaster);
                BuffInfo.RemoveBuff(target, BuffIcon.BloodOathCurse);

                return true;
            }

            return false;
        }

        public static Mobile GetBloodOath(Mobile m) =>
            m == null || _oathTable.TryGetValue(m, out var oath) && oath == m ? null : oath;

        private class ExpireTimer : Timer
        {
            private Mobile _target;
            private DateTime _end;

            public Mobile Caster { get; }

            public ExpireTimer(Mobile caster, Mobile target, TimeSpan delay) : base(
                TimeSpan.FromSeconds(1.0),
                TimeSpan.FromSeconds(1.0)
            )
            {
                Caster = caster;
                _target = target;
                _end = Core.Now + delay;
            }

            protected override void OnTick()
            {
                if (Caster.Deleted || _target.Deleted || !Caster.Alive || !_target.Alive ||
                    Core.Now >= _end)
                {
                    RemoveCurse(_target);
                }
            }
        }
    }
}
