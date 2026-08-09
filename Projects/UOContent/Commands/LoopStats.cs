#if EVENT_LOOP_PROFILING
using System;
using System.Globalization;
using System.IO;
using Server.Logging;

namespace Server.Commands;

/// <summary>
/// Reports the event-loop time decomposition recorded by <see cref="EventLoopProfiler"/>.
/// Only compiled when the server is built with -p:EventLoopProfiling=true.
/// See dev-docs/debugging-event-loop.md for how to read the output.
/// </summary>
public static class LoopStats
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LoopStats));

    public static void Configure()
    {
        CommandSystem.Register("LoopStats", AccessLevel.Administrator, LoopStats_OnCommand);
    }

    [Usage("LoopStats")]
    [Description("Summarizes the last minute of event-loop time accounting and writes the full history to a CSV.")]
    private static void LoopStats_OnCommand(CommandEventArgs e)
    {
        var history = EventLoopProfiler.History();
        if (history.Length == 0)
        {
            e.Mobile.SendMessage("No samples recorded yet.");
            return;
        }

        var window = Math.Min(60, history.Length);

        double wall = 0, sleep = 0, gc = 0, stolen = 0, stolenMax = 0;
        long iterations = 0, sleeps = 0, lateWakes = 0, wheelLagMax = 0;
        Span<double> phases = stackalloc double[EventLoopProfiler.PhaseCount];
        Span<double> phaseMax = stackalloc double[EventLoopProfiler.PhaseCount];

        for (var i = history.Length - window; i < history.Length; i++)
        {
            ref var s = ref history[i];
            wall += s.WallMs;
            sleep += s.SleepMs;
            gc += s.GcPauseMs;
            stolen += s.StolenMs;
            iterations += s.Iterations;
            sleeps += s.Sleeps;
            lateWakes += s.LateWakes;

            if (s.StolenMs > stolenMax)
            {
                stolenMax = s.StolenMs;
            }

            if (s.WheelLagMaxMs > wheelLagMax)
            {
                wheelLagMax = s.WheelLagMaxMs;
            }

            for (var p = 0; p < EventLoopProfiler.PhaseCount; p++)
            {
                phases[p] += s.Phases[p];
                if (s.Phases[p] > phaseMax[p])
                {
                    phaseMax[p] = s.Phases[p];
                }
            }
        }

        e.Mobile.SendMessage($"Loop, last {window}s of wall time {wall:F0}ms:");
        e.Mobile.SendMessage($"  sleep {100 * sleep / wall:F1}%, gc {100 * gc / wall:F1}%, stolen {100 * stolen / wall:F1}% (worst {stolenMax:F0}ms/s)");

        for (var p = 0; p < EventLoopProfiler.PhaseCount; p++)
        {
            e.Mobile.SendMessage($"  {(LoopPhase)p}: {100 * phases[p] / wall:F1}% (worst {phaseMax[p]:F0}ms/s)");
        }

        e.Mobile.SendMessage($"  {iterations} iterations, {sleeps} sleeps, {lateWakes} late wakes, worst wheel lag {wheelLagMax}ms");

        var path = Path.Combine(Core.BaseDirectory, $"loopstats-{Core.Now:yyyyMMdd-HHmmss}.csv");
        WriteCsv(path, history);
        e.Mobile.SendMessage($"Full history ({history.Length} samples) written to {path}");
        logger.Information("Loop stats dumped to {Path}", path);
    }

    private static void WriteCsv(string path, EventLoopProfiler.Sample[] history)
    {
        using var writer = new StreamWriter(path);
        writer.Write("wallStart,wallMs,iterations,sleeps,sleepMs,sleepOvershootMaxMs,lateWakes,wheelLagMaxMs,wakesIssued,wakesElided,gcPauseMs,gen0,gen1,gen2,stolenMs");
        for (var p = 0; p < EventLoopProfiler.PhaseCount; p++)
        {
            writer.Write(',');
            writer.Write((LoopPhase)p);
        }

        writer.WriteLine();

        for (var i = 0; i < history.Length; i++)
        {
            ref var s = ref history[i];
            writer.Write(string.Create(
                CultureInfo.InvariantCulture,
                $"{s.WallStart},{s.WallMs},{s.Iterations},{s.Sleeps},{s.SleepMs:F2},{s.SleepOvershootMaxMs:F2},{s.LateWakes},{s.WheelLagMaxMs},{s.WakesIssued},{s.WakesElided},{s.GcPauseMs:F2},{s.Gen0},{s.Gen1},{s.Gen2},{s.StolenMs:F2}"
            ));
            for (var p = 0; p < EventLoopProfiler.PhaseCount; p++)
            {
                writer.Write(',');
                writer.Write(string.Create(CultureInfo.InvariantCulture, $"{s.Phases[p]:F2}"));
            }

            writer.WriteLine();
        }
    }
}
#endif
