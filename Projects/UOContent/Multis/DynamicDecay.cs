using System;
using System.Collections.Generic;

namespace Server.Multis
{
    public static class DynamicDecay
    {
        private static readonly Dictionary<DecayLevel, DecayStageInfo> m_Stages;

        static DynamicDecay()
        {
            m_Stages = new Dictionary<DecayLevel, DecayStageInfo>();

            Register(DecayLevel.LikeNew, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
            Register(DecayLevel.Slightly, TimeSpan.FromDays(1), TimeSpan.FromDays(2));
            Register(DecayLevel.Somewhat, TimeSpan.FromDays(1), TimeSpan.FromDays(2));
            Register(DecayLevel.Fairly, TimeSpan.FromDays(1), TimeSpan.FromDays(2));
            Register(DecayLevel.Greatly, TimeSpan.FromDays(1), TimeSpan.FromDays(2));
            Register(DecayLevel.IDOC, TimeSpan.FromHours(12), TimeSpan.FromHours(24));
        }

        public static bool Enabled => Core.ML;

        public static void Register(DecayLevel level, TimeSpan min, TimeSpan max)
        {
            m_Stages[level] = new DecayStageInfo(min, max);
        }

        public static bool Decays(DecayLevel level) => m_Stages.ContainsKey(level);

        public static TimeSpan GetRandomDuration(DecayLevel level)
        {
            if (!m_Stages.TryGetValue(level, out var info))
            {
                return TimeSpan.Zero;
            }

            var min = info.MinDuration.Ticks;
            var max = info.MaxDuration.Ticks;

            return TimeSpan.FromTicks(min + (long)(Utility.RandomDouble() * (max - min)));
        }
    }

    public class DecayStageInfo
    {
        public DecayStageInfo(TimeSpan min, TimeSpan max)
        {
            MinDuration = min;
            MaxDuration = max;
        }

        public TimeSpan MinDuration { get; }

        public TimeSpan MaxDuration { get; }
    }
}
