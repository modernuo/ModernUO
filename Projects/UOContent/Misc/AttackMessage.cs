using System;
using Server.Collections;

namespace Server.Misc
{
    public static class AttackMessage
    {
        private const int Hue = 0x22;

        private static readonly TimeSpan Delay = TimeSpan.FromMinutes(1.0);

        public static void Initialize()
        {
            EventSink.AggressiveAction += EventSink_AggressiveAction;
        }

        public static void EventSink_AggressiveAction(AggressiveActionEventArgs e)
        {
            var aggressor = e.Aggressor;
            var aggressed = e.Aggressed;

            if (!aggressor.Player || !aggressed.Player)
            {
                return;
            }

            if (!CheckAggressions(aggressor, aggressed))
            {
                aggressor.LocalOverheadMessage(
                    MessageType.Regular,
                    Hue,
                    true,
                    $"You are attacking {aggressed.Name}!"
                );
                aggressed.LocalOverheadMessage(
                    MessageType.Regular,
                    Hue,
                    true,
                    $"{aggressor.Name} is attacking you!"
                );
            }
        }

        public static bool CheckAggressions(Mobile m1, Mobile m2)
        {
            foreach (var info in m1.Aggressors)
            {
                if (info.Attacker == m2 && Core.Now < info.LastCombatTime + Delay)
                {
                    return true;
                }
            }

            foreach (var info in m2.Aggressors)
            {
                if (info.Attacker == m1 && Core.Now < info.LastCombatTime + Delay)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
