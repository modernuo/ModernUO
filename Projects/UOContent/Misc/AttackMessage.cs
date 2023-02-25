using System;

namespace Server.Misc
{
    public static class AttackMessage
    {
        private const string AggressorFormat = "You are attacking {0}!";
        private const string AggressedFormat = "{0} is attacking you!";
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
                    string.Format(AggressorFormat, aggressed.Name)
                );
                aggressed.LocalOverheadMessage(
                    MessageType.Regular,
                    Hue,
                    true,
                    string.Format(AggressedFormat, aggressor.Name)
                );
            }
        }

        public static bool CheckAggressions(Mobile m1, Mobile m2)
        {
            var list = m1.Aggressors;

            for (var i = 0; i < list.Count; ++i)
            {
                var info = list[i];

                if (info.Attacker == m2 && Core.Now < info.LastCombatTime + Delay)
                {
                    return true;
                }
            }

            list = m2.Aggressors;

            for (var i = 0; i < list.Count; ++i)
            {
                var info = list[i];

                if (info.Attacker == m1 && Core.Now < info.LastCombatTime + Delay)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
