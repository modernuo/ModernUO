using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Mysticism
{
    public class SpellPlagueSpell : MysticSpell
    {
        private static readonly SpellInfo _info = new(
            "Spell Plague",
            "Vas Rel Jux Ort",
            -1,
            9002,
            Reagent.DaemonBone,
            Reagent.DragonsBlood,
            Reagent.Nightshade,
            Reagent.SulfurousAsh
        );

        private static readonly Dictionary<Mobile, SpellPlagueTimer> _table = new();

        public SpellPlagueSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Seventh;

        public static void Initialize()
        {
            EventSink.PlayerDeath += OnPlayerDeath;
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public void Target(Mobile targeted)
        {
            if (CheckHSequence(targeted))
            {
                SpellHelper.Turn(Caster, targeted);

                SpellHelper.CheckReflect(6, Caster, ref targeted);

                /* The target is hit with an explosion of chaos damage and then inflicted
                 * with the spell plague curse. Each time the target is damaged while under
                 * the effect of the spell plague, they may suffer an explosion of chaos
                 * damage. The initial chance to trigger the explosion starts at 90% and
                 * reduces by 30% every time an explosion occurs. Once the target is
                 * afflicted by 3 explosions or 8 seconds have passed, that spell plague
                 * is removed from the target. Spell Plague will stack with other spell
                 * plagues so that they are applied one after the other.
                 */

                VisualEffect(targeted);

                var damage = GetNewAosDamage(33, 1, 5, targeted);
                SpellHelper.Damage(this, targeted, damage, 0, 0, 0, 0, 0);

                var timer = new SpellPlagueTimer(this, targeted);

                if (_table.TryGetValue(targeted, out var oldtimer))
                {
                    oldtimer.SetNext(timer);
                }
                else
                {
                    _table[targeted] = timer;
                    timer.StartPlague();
                }
            }

            FinishSequence();
        }

        public static bool UnderEffect(Mobile m) => _table.ContainsKey(m);

        public static bool RemoveEffect(Mobile m)
        {
            if (_table.Remove(m, out var context))
            {
                context.Stop();
                BuffInfo.RemoveBuff(m, BuffIcon.SpellPlague);
                return true;
            }

            return false;
        }

        public static void CheckPlague(Mobile m)
        {
            if (_table.TryGetValue(m, out var context))
            {
                context.OnDamage();
            }
        }

        private static void OnPlayerDeath(Mobile m)
        {
            RemoveEffect(m);
        }

        protected void VisualEffect(Mobile to)
        {
            to.PlaySound(0x658);

            to.FixedParticles(0x3728, 1, 13, 0x26B8, 0x47E, 7, EffectLayer.Head, 0);
            to.FixedParticles(0x3779, 1, 15, 0x251E, 0x43, 7, EffectLayer.Head, 0);
        }

        private class SpellPlagueTimer : Timer
        {
            private readonly SpellPlagueSpell m_Owner;
            private readonly Mobile m_Target;
            private int m_Explosions;
            private DateTime m_LastExploded;
            private SpellPlagueTimer m_Next;

            public SpellPlagueTimer(SpellPlagueSpell owner, Mobile target) : base(TimeSpan.FromSeconds(8.0))
            {
                m_Owner = owner;
                m_Target = target;
            }

            public void SetNext(SpellPlagueTimer timer)
            {
                if (m_Next == null)
                {
                    m_Next = timer;
                }
                else
                {
                    m_Next.SetNext(timer);
                }
            }

            public void StartPlague()
            {
                BuffInfo.AddBuff(
                    m_Target,
                    new BuffInfo(BuffIcon.SpellPlague, 1031690, 1080167, TimeSpan.FromSeconds(8.5), m_Target)
                );

                Start();
            }

            public void OnDamage()
            {
                if (DateTime.Now <= m_LastExploded + TimeSpan.FromSeconds(2.0))
                {
                    return;
                }

                var exploChance = 90 - m_Explosions * 30;

                var resist = m_Target.Skills.MagicResist.Value;

                if (resist >= 70)
                {
                    exploChance -= (int)((resist - 70.0) * 3.0 / 10.0);
                }

                if (exploChance > Utility.Random(100))
                {
                    m_Owner.VisualEffect(m_Target);

                    var damage = m_Owner.GetNewAosDamage(15 + m_Explosions * 3, 1, 5, m_Target);

                    m_Explosions++;
                    m_LastExploded = DateTime.Now;

                    SpellHelper.Damage(m_Owner, m_Target, damage, 0, 0, 0, 0, 0, 100);

                    if (m_Explosions >= 3)
                    {
                        EndPlague();
                    }
                }
            }

            public void EndPlague()
            {
                if (m_Next != null)
                {
                    _table[m_Target] = m_Next;
                    m_Next.StartPlague();
                }
                else
                {
                    _table.Remove(m_Target);
                    BuffInfo.RemoveBuff(m_Target, BuffIcon.SpellPlague);
                }

                Stop();
            }
        }

        private class InternalTarget : Target
        {
            private readonly SpellPlagueSpell _owner;

            public InternalTarget(SpellPlagueSpell owner) : base(12, false, TargetFlags.Harmful) =>
                _owner = owner;

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Mobile mobile)
                {
                    _owner.Target(mobile);
                }
            }

            protected override void OnTargetFinish(Mobile from)
            {
                _owner.FinishSequence();
            }
        }
    }
}
