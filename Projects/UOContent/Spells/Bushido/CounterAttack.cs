using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Bushido
{
    public class CounterAttack : SamuraiSpell
    {
        private static readonly SpellInfo _info = new(
            "CounterAttack",
            null,
            -1,
            9002
        );

        private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();

        public CounterAttack(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.25);

        public override double RequiredSkill => 40.0;
        public override int RequiredMana => 5;

        public override bool CheckCast()
        {
            if (!base.CheckCast())
            {
                return false;
            }

            if (Caster.FindItemOnLayer(Layer.TwoHanded) is BaseShield)
            {
                return true;
            }

            if (Caster.FindItemOnLayer(Layer.OneHanded) is BaseWeapon)
            {
                return true;
            }

            if (Caster.FindItemOnLayer(Layer.TwoHanded) is BaseWeapon)
            {
                return true;
            }

            Caster.SendLocalizedMessage(1062944); // You must have a weapon or a shield equipped to use this ability!
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
                Caster.SendLocalizedMessage(1063118); // You prepare to respond immediately to the next blocked blow.

                OnCastSuccessful(Caster);

                StartCountering(Caster);
            }

            FinishSequence();
        }

        public static bool IsCountering(Mobile m) => _table.ContainsKey(m);

        private static bool StopCounterTimer(Mobile m)
        {
            if (_table.Remove(m, out var timerToken))
            {
                timerToken.Cancel();
                return true;
            }

            return false;
        }

        public static void StartCountering(Mobile m)
        {
            StopCounterTimer(m);

            Timer.StartTimer(TimeSpan.FromSeconds(30.0),
                () =>
                {
                    StopCountering(m);
                    m.SendLocalizedMessage(1063119); // You return to your normal stance.
                },
                out var timerToken
            );

            _table[m] = timerToken;
        }

        public static void StopCountering(Mobile m)
        {
            if (StopCounterTimer(m))
            {
                OnEffectEnd(m, typeof(CounterAttack));
            }
        }
    }
}
