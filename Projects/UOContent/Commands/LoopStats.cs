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
        var snapshot = Capture();
        logger.Information(
            "loop: cpu={Cpu:F1}% cps={Cps:F0} tickLagPeak={PeakLag}ms tickLagNow={LastLag}ms idleWait={IdleWait}ms",
            snapshot.CpuPercent,
            snapshot.AverageCps,
            snapshot.PeakTickLag,
            snapshot.LastTickLag,
            Core.EventLoopIdleWaitMs
        );
    }

    private readonly record struct Snapshot(
        double CpuPercent,
        double AverageCps,
        long PeakTickLag,
        long LastTickLag
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

        var snapshot = new Snapshot(cpuPercent, Core.AverageCPS, Timer.PeakTickLag, Timer.LastTickLag);

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

        e.Mobile.SendMessage($"{"Event loop"}: {Core.EventLoopIdleWaitMs switch
        {
            <= 0 => "legacy spin",
            var ms => $"idle wait {ms}ms"
        }}");
        e.Mobile.SendMessage($"{"CPU"}: {snapshot.CpuPercent:F1}{"% of one core since last check"}");
        e.Mobile.SendMessage($"{"Cycles/sec"}: {snapshot.AverageCps:F0}");
        e.Mobile.SendMessage($"{"Tick lag"}: {snapshot.LastTickLag}{"ms now, peak "}{snapshot.PeakTickLag}{"ms"}");
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
