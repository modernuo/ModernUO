using System;
using Server.Mobiles;
using Server.Spells.Ninjitsu;

namespace Server.Misc
{
    public enum DFAlgorithm
    {
        Standard,
        PainSpike
    }

    public static class WeightOverloading
    {
        public const int OverloadAllowance = 4; // We can be four stones overweight without getting fatigued

        public static DFAlgorithm DFA { get; set; }

        public static void Initialize()
        {
            EventSink.Movement += EventSink_Movement;
        }

        public static void FatigueOnDamage(Mobile m, int damage)
        {
            var fatigue = DFA switch
            {
                DFAlgorithm.Standard  => damage * (100.0 / m.Hits) * ((double)m.Stam / 100) - 5.0,
                DFAlgorithm.PainSpike => damage * (100.0 / m.Hits + (50.0 + m.Stam) / 100 - 1.0) - 5.0,
                _                     => 0.0
            };

            if (fatigue > 0)
            {
                m.Stam -= (int)fatigue;
            }
        }

        public static int GetMaxWeight(Mobile m) => m.MaxWeight;

        public static void EventSink_Movement(MovementEventArgs e)
        {
            var from = e.Mobile;

            if (!from.Alive || from.AccessLevel > AccessLevel.Player)
            {
                return;
            }

            if (!from.Player)
            {
                // Else it won't work on monsters.
                DeathStrike.AddStep(from);
                return;
            }

            var maxWeight = GetMaxWeight(from) + OverloadAllowance;
            var overWeight = Mobile.BodyWeight + from.TotalWeight - maxWeight;

            if (overWeight > 0)
            {
                from.Stam -= GetStamLoss(from, overWeight, (e.Direction & Direction.Running) != 0);

                if (from.Stam == 0)
                {
                    from.SendLocalizedMessage(
                        500109
                    ); // You are too fatigued to move, because you are carrying too much weight!
                    e.Blocked = true;
                    return;
                }
            }

            if (from.Stam * 100 / Math.Max(from.StamMax, 1) < 10)
            {
                --from.Stam;
            }

            if (!Core.AOS && from.Stam == 0)
            {
                from.SendLocalizedMessage(500110); // You are too fatigued to move.
                e.Blocked = true;
                return;
            }

            if (from is PlayerMobile pm)
            {
                var amt = pm.Mounted ? 48 : 16;

                if (++pm.StepsTaken % amt == 0)
                {
                    --pm.Stam;
                }
            }

            DeathStrike.AddStep(from);
        }

        public static int GetStamLoss(Mobile from, int overWeight, bool running)
        {
            var loss = 5 + overWeight / 25;

            if (from.Mounted)
            {
                loss /= 3;
            }

            if (running)
            {
                loss *= 2;
            }

            return loss;
        }

        public static bool IsOverloaded(Mobile m)
        {
            if (!m.Player || !m.Alive || m.AccessLevel > AccessLevel.Player)
            {
                return false;
            }

            return Mobile.BodyWeight + m.TotalWeight > GetMaxWeight(m) + OverloadAllowance;
        }
    }
}
