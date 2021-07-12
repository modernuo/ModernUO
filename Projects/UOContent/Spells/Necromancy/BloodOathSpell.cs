using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
    public class BloodOathSpell : NecromancerSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo m_Info = new(
            "Blood Oath",
            "In Jux Mani Xen",
            203,
            9031,
            Reagent.DaemonBlood
        );

        private static readonly Dictionary<Mobile, Mobile> m_OathTable = new();
        private static readonly Dictionary<Mobile, ExpireTimer> m_Table = new();

        public BloodOathSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
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
            else if (Caster == m || !(m is PlayerMobile || m is BaseCreature))
            {
                Caster.SendLocalizedMessage(1060508); // You can't curse that.
            }
            else if (m_OathTable.ContainsKey(Caster))
            {
                Caster.SendLocalizedMessage(1061607); // You are already bonded in a Blood Oath.
            }
            else if (m_OathTable.ContainsKey(m))
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

                m_Table.TryGetValue(m, out var timer);
                timer?.DoExpire();

                m_OathTable[Caster] = Caster;
                m_OathTable[m] = Caster;

                m.Spell?.OnCasterHurt();

                Caster.PlaySound(0x175);

                Caster.FixedParticles(0x375A, 1, 17, 9919, 33, 7, EffectLayer.Waist);
                Caster.FixedParticles(0x3728, 1, 13, 9502, 33, 7, (EffectLayer)255);

                m.FixedParticles(0x375A, 1, 17, 9919, 33, 7, EffectLayer.Waist);
                m.FixedParticles(0x3728, 1, 13, 9502, 33, 7, (EffectLayer)255);

                var duration = TimeSpan.FromSeconds((GetDamageSkill(Caster) - GetResistSkill(m)) / 8 + 8);
                m.CheckSkill(SkillName.MagicResist, 0.0, 120.0); // Skill check for gain

                timer = new ExpireTimer(Caster, m, duration);
                timer.Start();

                BuffInfo.AddBuff(Caster, new BuffInfo(BuffIcon.BloodOathCaster, 1075659, duration, Caster, m.Name));
                BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.BloodOathCurse, 1075661, duration, m, Caster.Name));

                m_Table[m] = timer;
                HarmfulSpell(m);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }

        public static void RemoveCurse(Mobile m)
        {
            m_Table.TryGetValue(m, out var t);
            t?.DoExpire();
        }

        public static Mobile GetBloodOath(Mobile m) =>
            m == null || m_OathTable.TryGetValue(m, out var oath) && oath == m ? null : oath;

        private class ExpireTimer : Timer
        {
            private readonly Mobile m_Caster;
            private readonly DateTime m_End;
            private readonly Mobile m_Target;

            public ExpireTimer(Mobile caster, Mobile target, TimeSpan delay) : base(
                TimeSpan.FromSeconds(1.0),
                TimeSpan.FromSeconds(1.0)
            )
            {
                m_Caster = caster;
                m_Target = target;
                m_End = Core.Now + delay;
            }

            protected override void OnTick()
            {
                if (m_Caster.Deleted || m_Target.Deleted || !m_Caster.Alive || !m_Target.Alive ||
                    Core.Now >= m_End)
                {
                    DoExpire();
                }
            }

            public void DoExpire()
            {
                if (m_OathTable.Remove(m_Caster))
                {
                    m_Caster.SendLocalizedMessage(1061620); // Your Blood Oath has been broken.
                }

                if (m_OathTable.Remove(m_Target))
                {
                    m_Target.SendLocalizedMessage(1061620); // Your Blood Oath has been broken.
                }

                Stop();

                BuffInfo.RemoveBuff(m_Caster, BuffIcon.BloodOathCaster);
                BuffInfo.RemoveBuff(m_Target, BuffIcon.BloodOathCurse);

                m_Table.Remove(m_Caster);
            }
        }
    }
}
