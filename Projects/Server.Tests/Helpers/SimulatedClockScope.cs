using System;
using Server.Items;

namespace Server.Tests;

/// <summary>
/// Lets a test move the game clock (<see cref="Core.Now"/>) and restores it on dispose, so a failed
/// assert cannot leave every later test running in the future.
/// </summary>
internal sealed class SimulatedClockScope : IDisposable
{
    private static readonly TimeSpan _decayTickStep = TimeSpan.FromMilliseconds(256);

    public SimulatedClockScope() => Start = Core._now;

    public DateTime Start { get; }

    public DateTime Now
    {
        get => Core._now;
        set => Core._now = value;
    }

    /// <summary>
    /// Steps the clock from <see cref="Now"/> through <paramref name="duration"/>, driving the decay
    /// scheduler each step without the shared timer wheel, and stops early once <paramref name="item"/>
    /// is deleted.
    /// </summary>
    public void AdvanceDecay(TimeSpan duration, Item item)
    {
        var start = Core._now;
        var deadline = start + duration;

        for (var now = start; now <= deadline && !item.Deleted; now += _decayTickStep)
        {
            Core._now = now;
            DecayScheduler.ProcessTick(now);
        }
    }

    public void Dispose() => Core._now = Start;
}
