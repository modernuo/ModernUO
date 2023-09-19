using System;

namespace Server.Mobiles;

public interface IHasSteps
{
    int StepsMax { get; }

    int StepsGainedPerIdleTime { get; }

    TimeSpan IdleTimePerStepsGain { get; }
}
