using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Spells.Chivalry
{
    public class EnemyOfOneSpell : PaladinSpell
    {
        private static readonly SpellInfo _info = new(
            "Enemy of One",
            "Forul Solum",
            -1,
            9002
        );

        private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();

        public EnemyOfOneSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.5);

        public override double RequiredSkill => 45.0;
        public override int RequiredMana => 20;
        public override int RequiredTithing => 10;
        public override int MantraNumber => 1060723; // Forul Solum
        public override bool BlocksMovement => false;

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.PlaySound(0x0F5);
                Caster.PlaySound(0x1ED);
                Caster.FixedParticles(0x375A, 1, 30, 9966, 33, 2, EffectLayer.Head);
                Caster.FixedParticles(0x37B9, 1, 30, 9502, 43, 3, EffectLayer.Head);

                RemoveTimer(Caster);

                var delay = Math.Clamp(ComputePowerValue(1) / 60.0, 1.5, 3.5);

                Timer.StartTimer(TimeSpan.FromMinutes(delay), () => Expire_Callback(Caster), out var timerToken);

                _table[Caster] = timerToken;

                if (Caster is PlayerMobile mobile)
                {
                    mobile.EnemyOfOneType = null;
                    mobile.WaitingForEnemy = true;

                    BuffInfo.AddBuff(
                        mobile,
                        new BuffInfo(BuffIcon.EnemyOfOne, 1075653, 1044111, TimeSpan.FromMinutes(delay), mobile)
                    );
                }
            }

            FinishSequence();
        }

        private static void RemoveTimer(Mobile m)
        {
            if (_table.Remove(m, out var timerToken))
            {
                timerToken.Cancel();
            }
        }

        private static void Expire_Callback(Mobile m)
        {
            RemoveTimer(m);

            m.PlaySound(0x1F8);

            if (m is PlayerMobile mobile)
            {
                mobile.EnemyOfOneType = null;
                mobile.WaitingForEnemy = false;
            }
        }
    }
}
