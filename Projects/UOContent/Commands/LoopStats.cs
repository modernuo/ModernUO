using System;
using Server.Logging;

namespace Server.Commands;

/// <summary>
/// Event loop health reporting, for comparing scheduler behaviour on real hardware.
/// </summary>
/// <remarks>
/// Deliberately cheap. The metrics operators actually want -- how much CPU the process is using
/// and how far behind the timer wheel is running -- are available without enumerating processes or
/// threads. <see cref="Environment.CpuUsage"/> reads the process' own accounting; tick lag reuses
/// arithmetic the wheel already performs. Hand-rolled probes that call <c>Process.Threads</c> on a
/// timer cost several percent of the main thread and have been observed causing the very stalls
/// they were added to diagnose.
/// </remarks>
public static class LoopStats
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(LoopStats));

    private static TimeSpan _lastCpu;
    private static DateTime _lastReport;
    private static bool _reporting;
    private static long _lastSkippedTicks;
    private static long _lastTurns;
    private static long _lastMissed8;
    private static long _lastMissed16;
    private static long _lastSchedMissed;
    private static long _lastIterations;
    private static long _lastSleeps;

    // Anything shorter than most of the configured interval is a catch-up duplicate, not a window.
    private static TimeSpan _minReportWindow;

    public static void Configure()
    {
        CommandSystem.Register("LoopStats", AccessLevel.Administrator, LoopStats_OnCommand);
        CommandSystem.Register("LoopStatsLog", AccessLevel.Administrator, LoopStatsLog_OnCommand);

        // Opt-in periodic logging, so a measurement run needs no in-game interaction. 0 = off.
        var interval = ServerConfiguration.GetOrUpdateSetting("server.loopStatsIntervalSeconds", 0);
        if (interval > 0)
        {
            StartReporting(TimeSpan.FromSeconds(interval));
        }
    }

    /// <summary>
    /// Starts periodic logging of loop health, used to capture a measurement window.
    /// </summary>
    public static void StartReporting(TimeSpan interval)
    {
        if (_reporting)
        {
            return;
        }

        _reporting = true;
        _minReportWindow = interval * 0.75;
        _lastCpu = Environment.CpuUsage.TotalTime;
        _lastReport = Core.Now;
        Timer.ResetPeakTickLag();

        Timer.DelayCall(interval, interval, Report);

        logger.Information(
            "loop stats: reporting every {Interval}s (idleWait={IdleWait}ms)",
            interval.TotalSeconds,
            Core.EventLoopIdleWaitMs
        );
    }

    private static void Report()
    {
        // The reporter is itself a wheel timer, so a stall makes it come due once per slot the
        // wheel then catches up on -- a 183 second gap at startup produced twelve reports in the
        // same millisecond, each covering no elapsed time and printing zeroes. Keep the first,
        // which legitimately describes the stall, and drop the rest of the burst.
        if (Core.Now - _lastReport < _minReportWindow)
        {
            return;
        }

        var snapshot = Capture();
        logger.Information(
            "loop: cpu={Cpu:F1}% cps={Cps:F0} missed8ms={Missed8} missed16ms={Missed16} sched={SchedMissed} " +
            "slots={Skipped}/{Turns} tickLagPeak={PeakLag}ms " +
            "sleeps={Sleeps}/{Iterations} ({SleepRatio:F1}%) wakes={WakesIssued} elided={WakesElided} " +
            "backoffs={Backoffs}{Suspended} idleWait={IdleWait}ms",
            snapshot.CpuPercent,
            snapshot.AverageCps,
            snapshot.MissedTickDeadlines,
            snapshot.MissedFrameDeadlines,
            snapshot.SchedulerMissedFrames,
            snapshot.SkippedTicks,
            snapshot.TotalTurns,
            snapshot.PeakTickLag,
            snapshot.Sleeps,
            snapshot.Iterations,
            snapshot.SleepRatio,
            snapshot.WakesIssued,
            snapshot.WakesElided,
            Core.IdleSleepBackoffs,
            Core.IdleSleepSuspended ? " (SUSPENDED)" : "",
            Core.EventLoopIdleWaitMs
        );
    }

    private readonly record struct Snapshot(
        double CpuPercent,
        double AverageCps,
        long PeakTickLag,
        long LastTickLag,
        long SkippedTicks,
        long TotalTurns,
        long MissedTickDeadlines,
        long MissedFrameDeadlines,
        long SchedulerMissedFrames,
        long Sleeps,
        long Iterations,
        double SleepRatio,
        long WakesIssued,
        long WakesElided
    );

    /// <summary>
    /// Samples CPU usage since the previous call and resets the tick-lag high-water mark, so each
    /// report describes its own window rather than being pinned by an old spike.
    /// </summary>
    private static Snapshot Capture()
    {
        var cpu = Environment.CpuUsage.TotalTime;
        var now = Core.Now;

        var wallMs = (now - _lastReport).TotalMilliseconds;
        var cpuMs = (cpu - _lastCpu).TotalMilliseconds;

        // Guard the very first call, where the window is zero-length.
        var cpuPercent = wallMs > 0 ? cpuMs / wallMs * 100 : 0;

        // Core's counters are monotonic for the same reason Timer's are: the loop's own health
        // sample reads them too. Difference rather than reset.
        var iterationsTotal = Core.LoopIterations;
        var sleepsTotal = Core.LoopSleeps;
        var iterations = iterationsTotal - _lastIterations;
        var sleeps = sleepsTotal - _lastSleeps;
        _lastIterations = iterationsTotal;
        _lastSleeps = sleepsTotal;

        var sleepRatio = iterations > 0 ? (double)sleeps / iterations * 100 : 0;

        // Timer's counters are monotonic because the loop's backoff reads them too, so difference
        // them here rather than resetting and stealing the other reader's baseline.
        var skippedTotal = Timer.SkippedTicks;
        var turnsTotal = Timer.TotalTurns;
        var missed8Total = Timer.MissedTickDeadlines;
        var missed16Total = Timer.MissedFrameDeadlines;
        var skipped = skippedTotal - _lastSkippedTicks;
        var turns = turnsTotal - _lastTurns;
        var schedMissedTotal = Core.SchedulerMissedFrames;
        var missed8 = missed8Total - _lastMissed8;
        var missed16 = missed16Total - _lastMissed16;
        var schedMissed = schedMissedTotal - _lastSchedMissed;
        _lastSkippedTicks = skippedTotal;
        _lastTurns = turnsTotal;
        _lastMissed8 = missed8Total;
        _lastMissed16 = missed16Total;
        _lastSchedMissed = schedMissedTotal;

        var snapshot = new Snapshot(
            cpuPercent,
            Core.AverageCPS,
            Timer.PeakTickLag,
            Timer.LastTickLag,
            skipped,
            turns,
            missed8,
            missed16,
            schedMissed,
            sleeps,
            iterations,
            sleepRatio,
            EventLoopContext.WakesIssued,
            EventLoopContext.WakesElided
        );

        _lastCpu = cpu;
        _lastReport = now;
        Timer.ResetPeakTickLag();

        return snapshot;
    }

    [Usage("LoopStats")]
    [Description("Reports event loop CPU usage and timer tick lag since the previous call.")]
    private static void LoopStats_OnCommand(CommandEventArgs e)
    {
        var snapshot = Capture();

        var mode = Core.IdleSleepSuspended
            ? "spinning (backed off)"
            : Core.EventLoopIdleWaitMs <= 0
                ? "spinning (sleeping disabled)"
                : $"idle wait {Core.EventLoopIdleWaitMs}ms";

        e.Mobile.SendMessage($"{"Event loop"}: {mode}");
        e.Mobile.SendMessage($"{"CPU"}: {snapshot.CpuPercent:F1}{"% of one core since last check"}");
        e.Mobile.SendMessage($"{"Missed 16ms budget"}: {snapshot.MissedFrameDeadlines}{" ("}{snapshot.SchedulerMissedFrames}{" after a sleep)"}");
        e.Mobile.SendMessage($"{"Missed 8ms budget"}: {snapshot.MissedTickDeadlines}{" of "}{snapshot.TotalTurns}{" turns"}");
        e.Mobile.SendMessage($"{"Slots lost"}: {snapshot.SkippedTicks}{", peak lag "}{snapshot.PeakTickLag}{"ms"}");
        e.Mobile.SendMessage($"{"Cycles/sec"}: {snapshot.AverageCps:F0}{"  (paced by idle wait, not health)"}");
        e.Mobile.SendMessage($"{"Slept"}: {snapshot.Sleeps}{" of "}{snapshot.Iterations}{" iterations ("}{snapshot.SleepRatio:F1}{"%)"}");
        e.Mobile.SendMessage($"{"Wakes"}: {snapshot.WakesIssued}{" signalled, "}{snapshot.WakesElided}{" elided on-thread"}");
    }

    [Usage("LoopStatsLog [seconds]")]
    [Description("Starts periodic logging of event loop health. Defaults to every 30 seconds.")]
    private static void LoopStatsLog_OnCommand(CommandEventArgs e)
    {
        var seconds = e.Length > 0 ? e.GetInt32(0) : 30;
        if (seconds < 1)
        {
            e.Mobile.SendMessage("Interval must be at least 1 second.");
            return;
        }

        if (_reporting)
        {
            e.Mobile.SendMessage("Loop stats logging is already running.");
            return;
        }

        StartReporting(TimeSpan.FromSeconds(seconds));
        e.Mobile.SendMessage($"{"Logging event loop stats every "}{seconds}{" seconds."}");
    }
}
