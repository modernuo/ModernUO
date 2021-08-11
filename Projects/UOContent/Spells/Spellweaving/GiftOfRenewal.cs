using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Spellweaving
{
    public class GiftOfRenewalSpell : ArcanistSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo m_Info = new(
            "Gift of Renewal",
            "Olorisstra",
            -1
        );

        private static readonly Dictionary<Mobile, GiftOfRenewalTimer> m_Table = new();

        public GiftOfRenewalSpell(Mobile caster, Item scroll = null)
            : base(caster, scroll, m_Info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(3.0);

        public override double RequiredSkill => 0.0;
        public override int RequiredMana => 24;

        public void Target(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (m_Table.ContainsKey(m))
            {
                Caster.SendLocalizedMessage(501775); // This spell is already in effect.
            }
            else if (!Caster.CanBeginAction<GiftOfRenewalSpell>())
            {
                Caster.SendLocalizedMessage(501789); // You must wait before trying again.
            }
            else if (CheckBSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                Caster.FixedEffect(0x374A, 10, 20);
                Caster.PlaySound(0x5C9);

                if (m.Poisoned)
                {
                    m.CurePoison(m);
                }
                else
                {
                    var skill = Caster.Skills.Spellweaving.Value;

                    var hitsPerRound = 5 + (int)(skill / 24) + FocusLevel;
                    var duration = 30 + FocusLevel * 10;

                    var t = new GiftOfRenewalTimer(Caster, m, hitsPerRound, duration);

                    m_Table[m] = t;

                    t.Start();

                    Caster.BeginAction<GiftOfRenewalSpell>();

                    BuffInfo.AddBuff(
                        m,
                        new BuffInfo(BuffIcon.GiftOfRenewal, 1031602, 1075797, TimeSpan.FromSeconds(duration), m, hitsPerRound.ToString())
                    );
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Beneficial, 10);
        }

        public static bool StopEffect(Mobile m)
        {
            BuffInfo.RemoveBuff(m, BuffIcon.GiftOfRenewal);

            if (m_Table.Remove(m, out var timer))
            {
                timer.Stop();
                Timer.StartTimer(TimeSpan.FromSeconds(60), timer.m_Caster.EndAction<GiftOfRenewalSpell>);
                return true;
            }

            return false;
        }

        private class GiftOfRenewalTimer : Timer
        {
            public readonly Mobile m_Caster;
            public readonly int m_HitsPerRound;
            public readonly Mobile m_Mobile;

            internal GiftOfRenewalTimer(Mobile caster, Mobile mobile, int hitsPerRound, int duration)
                : base(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0), duration / 2)
            {
                m_Caster = caster;
                m_Mobile = mobile;
                m_HitsPerRound = hitsPerRound;
            }

            protected override void OnTick()
            {
                if (Index + 1 == Count)
                {
                    StopEffect(m_Mobile);
                    m_Mobile.PlaySound(0x455);
                    m_Mobile.SendLocalizedMessage(1075071); // The Gift of Renewal has faded.
                    return;
                }

                var m = m_Mobile;

                if (!m_Table.ContainsKey(m))
                {
                    Stop();
                    return;
                }

                if (!m.Alive)
                {
                    Stop();
                    StopEffect(m);
                    return;
                }

                if (m.Hits >= m.HitsMax)
                {
                    return;
                }

                var toHeal = m_HitsPerRound;

                SpellHelper.Heal(toHeal, m, m_Caster);
                m.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
            }
        }
    }
}
