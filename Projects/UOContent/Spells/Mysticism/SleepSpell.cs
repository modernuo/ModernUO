using System;
using System.Collections.Generic;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Mysticism
{
    public class SleepSpell : MysticSpell, ISpellTargetingMobile
    {
        public override SpellCircle Circle => SpellCircle.Third;

        private static readonly SpellInfo _info = new(
            "Sleep", "In Zu",
            230,
            9022,
            Reagent.Nightshade,
            Reagent.SpidersSilk,
            Reagent.BlackPearl
        );

        private static readonly Dictionary<Mobile, SleepTimer> _table = new();
        private static readonly HashSet<Mobile> _immunity = new();

        public SleepSpell(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful);
        }

        public void Target(Mobile m)
        {
            if (m.Paralyzed)
            {
                Caster.SendLocalizedMessage(1080134); // Your target is already immobilized and cannot be slept.
            }
            else if (_immunity.Contains(m))
            {
                Caster.SendLocalizedMessage(1080135); // Your target cannot be put to sleep.
            }
            else if (CheckHSequence(m))
            {
                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                var skill = (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 20;
                double duration = skill + 2 - GetResistSkill(m) / 10;

                // TODO: StoneForm immune
                if (duration <= 0 /*|| StoneFormSpell.CheckImmunity(m)*/)
                {
                    Caster.SendLocalizedMessage(1080136); // Your target resists sleep.
                    m.SendLocalizedMessage(1080137); // You resist sleep.
                }
                else
                {
                    DoSleep(m, TimeSpan.FromSeconds(duration));
                }
            }

            FinishSequence();
        }

        public static void DoSleep(Mobile target, TimeSpan duration)
        {
            target.Combatant = null;
            target.NetState.SendSpeedControl(SpeedControlSetting.Walk);

            if (_table.TryGetValue(target, out var timer))
            {
                timer.Stop();
            }

            timer = new SleepTimer(target, (long)duration.TotalMilliseconds);
            timer.Start();
            _table[target] = timer;

            BuffInfo.AddBuff(target, new BuffInfo(BuffIcon.Sleep, 1080139, 1080140, duration, target));

            target.Delta(MobileDelta.WeaponDamage);
        }

        public static bool UnderEffect(Mobile from) => _table.ContainsKey(from);

        public static bool EndSleep(Mobile m)
        {
            if (_table.Remove(m, out var timer))
            {
                m.NetState.SendSpeedControl(SpeedControlSetting.Disable);

                timer.Stop();

                BuffInfo.RemoveBuff(m, BuffIcon.Sleep);

                _immunity.Add(m);
                Timer.StartTimer(TimeSpan.FromSeconds(m.Skills[SkillName.MagicResist].Value / 10), () => _immunity.Remove(m));

                m.Delta(MobileDelta.WeaponDamage);
                return true;
            }

            return false;
        }

        private class SleepTimer : Timer
        {
            private readonly Mobile _mobile;
            private readonly long _end;

            public SleepTimer(Mobile target, long time) : base(TimeSpan.Zero, TimeSpan.FromSeconds(0.5))
            {
                _end = Core.TickCount + time;
                _mobile = target;
            }

            protected override void OnTick()
            {
                if (Core.TickCount >= _end)
                {
                    EndSleep(_mobile);
                    Stop();
                }
                else
                {
                    Effects.SendTargetParticles(_mobile, 0x3779, 1, 32, 0x13BA, EffectLayer.Head);
                }
            }
        }
    }
}
