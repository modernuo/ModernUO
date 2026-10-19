using System;
using System.Reflection;

namespace Server.Tests;

/// <summary>
/// Gives a test its own simulated clock and an empty timer wheel, and puts the process-wide ones
/// back on dispose. Core's tick, Core.Now and the wheel are statics shared by every test in the
/// host, so a test that resets them without restoring hands its clock to whatever runs next.
/// </summary>
/// <remarks>
/// Timers started inside the scope are stopped on dispose; timers that were scheduled before it
/// resume on the restored wheel.
/// </remarks>
internal sealed class TimerWheelScope : IDisposable
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    private static readonly FieldInfo _ringsField = GetTimerField("_rings");
    private static readonly FieldInfo _ringIndexesField = GetTimerField("_ringIndexes");
    private static readonly FieldInfo _lastTickTurnedField = GetTimerField("_lastTickTurned");

    private readonly long _savedTick;
    private readonly DateTime _savedNow;
    private readonly Timer[][] _savedRings;
    private readonly int[] _savedRingIndexes;
    private readonly long _savedLastTickTurned;
    private bool _disposed;

    public TimerWheelScope(long startTick = 0)
    {
        _savedTick = Core._tickCount;
        _savedNow = Core._now;
        _savedRings = (Timer[][])Rings.Clone();
        _savedRingIndexes = (int[])RingIndexes.Clone();
        _savedLastTickTurned = (long)_lastTickTurnedField.GetValue(null)!;

        Core._tickCount = startTick;
        Timer.Init(startTick);
    }

    private static Timer[][] Rings => (Timer[][])_ringsField.GetValue(null)!;

    private static int[] RingIndexes => (int[])_ringIndexesField.GetValue(null)!;

    /// <summary>The wheel's tick rate: one turn per call keeps it in lockstep with Core.TickCount.</summary>
    public static int Step => Timer.TickRate;

    /// <summary>Advances the clock by <paramref name="ms"/>, turning the wheel every step.</summary>
    public static void RunFor(long ms)
    {
        var deadline = Core._tickCount + ms;

        while (Core._tickCount - deadline < 0)
        {
            Core._tickCount += Step;
            Timer.Slice(Core._tickCount);
        }
    }

    /// <summary>
    /// Advances the clock step by step until <paramref name="condition"/> holds or
    /// <paramref name="maxMs"/> has passed. Returns the condition's final value.
    /// </summary>
    public static bool RunUntil(Func<bool> condition, long maxMs)
    {
        var deadline = Core._tickCount + maxMs;

        while (Core._tickCount - deadline < 0)
        {
            if (condition())
            {
                return true;
            }

            Core._tickCount += Step;
            Timer.Slice(Core._tickCount);
        }

        return condition();
    }

    /// <summary>Jumps the tick and Core.Now forward together, then fires everything that came due.</summary>
    public static void Advance(TimeSpan time)
    {
        Core._now += time;
        Core._tickCount += (long)time.TotalMilliseconds;
        Timer.Slice(Core._tickCount);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAll(Rings);

        var rings = Rings;
        for (var i = 0; i < rings.Length; i++)
        {
            rings[i] = _savedRings[i];
        }

        Array.Copy(_savedRingIndexes, RingIndexes, _savedRingIndexes.Length);
        _lastTickTurnedField.SetValue(null, _savedLastTickTurned);

        Core._tickCount = _savedTick;
        Core._now = _savedNow;
    }

    // Stop() unlinks the slot's head and promotes the next timer, so draining heads empties a slot.
    private static void StopAll(Timer[][] rings)
    {
        for (var i = 0; i < rings.Length; i++)
        {
            var ring = rings[i];
            for (var slot = 0; slot < ring.Length; slot++)
            {
                Timer head;
                while ((head = ring[slot]) != null)
                {
                    head.Stop();

                    if (ring[slot] == head)
                    {
                        // A head that was already stopped never unlinks itself.
                        ring[slot] = null;
                    }
                }
            }
        }
    }

    private static FieldInfo GetTimerField(string name) =>
        typeof(Timer).GetField(name, PrivateStatic)
        ?? throw new InvalidOperationException($"Timer.{name} not found via reflection; the timer wheel was refactored.");
}
