/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Main.cs                                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Server.Compression;
using Server.Json;
using Server.Logging;
using Server.Network;
using Server.Network.Bans;
using Server.Text;

namespace Server;

public static class Core
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(Core));

    // Written off-loop (Kill, RequestSnapshot); volatile because the loop blocks between reads.
    private static volatile bool _performProcessKill;
    private static bool _restartOnKill;
    private static volatile bool _performSnapshot;
    private static string _snapshotPath;

    // 0 = relaunch in-process. Otherwise a restart exits with this code and leaves the relaunch to a
    // supervisor; a spawned child loses the console and dies with a systemd unit or container.
    private static int _restartExitCode;

    // A backstop, not a latency control: the wheel's tick rate already bounds the sleep.
    // Measured across 1/2/4/8ms; 2 is optimal.
    private static int _eventLoopIdleWaitMs = 2;

    /// <summary>
    /// Longest the loop will block while idle, in milliseconds. 0 spins instead; the backoff
    /// does the same temporarily when the host keeps returning waits late.
    /// </summary>
    public static int EventLoopIdleWaitMs => _eventLoopIdleWaitMs;

    /// <summary>
    /// True when idle sleeping was disabled at startup because the host cannot honor short
    /// waits, overriding whatever <c>server.eventLoopIdleWaitMs</c> was configured to.
    /// </summary>
    public static bool IdleSleepUnsupported { get; private set; }

    /// <summary>
    /// Whether idle sleeping is currently suspended because the host returned waits late.
    /// </summary>
    /// <remarks>
    /// Compared by subtraction, never directly: tick counts can start enormous and wrap.
    /// See dev-docs/tick-counts.md.
    /// </remarks>
    public static bool IdleSleepSuspended => _tickCount - _idleSleepSuspendedUntil < 0;

    private const long HealthSampleIntervalMs = 1000;

    // Doubling: a fixed suspension oscillates forever on a persistently bad host, while doubling
    // converges on "stop sleeping" yet still recovers from a transient.
    private const long BackoffBaseMs = 5000;
    private const long BackoffMaxMs = 120_000;
    private const int BackoffMaxShift = 5;

    // Clean streak that clears the escalation.
    private const long BackoffResetAfterCleanMs = 60_000;

    // Below this a backoff is still recoverable and not actionable, so it only logs at Debug.
    private const int WarnAfterConsecutiveBackoffs = 3;

    // A sleep is bounded by the next wheel turn, so only a wait returning late can cost a deadline.
    // Measured per sleep, which is why server work (saves, heavy commands) cannot trip the backoff.
    private static int _lateWakes;

    // Denominator for the late-wake rate.
    private static int _sleepAttempts;

    private static long _nextHealthSample;
    private static long _idleSleepSuspendedUntil;
    private static int _lateWakeThreshold = 1;
    private static int _lateWakePercent = 10;
    private static long _idleSleepBackoffs;
    private static int _consecutiveBadSamples;
    private static int _consecutiveBackoffs;
    private static long _currentBackoffMs = BackoffBaseMs;
    private static long _lastBackoffAt;
    private static bool _loggedBackoffCeiling;

    /// <summary>
    /// Once a second, suspends idle sleeping (with escalating duration) if the host keeps
    /// returning idle waits a full tick or more late.
    /// </summary>
    private static void CheckSchedulerHealth()
    {
        if (_tickCount - _nextHealthSample < 0)
        {
            return;
        }

        _nextHealthSample = _tickCount + HealthSampleIntervalMs;

        var late = _lateWakes;
        var sleeps = _sleepAttempts;
        _lateWakes = 0;
        _sleepAttempts = 0;

        // A clean streak resets the escalation and re-arms the ceiling Error. Gated on the count
        // rather than a "_lastBackoffAt > 0" sentinel because tick counts are not guaranteed positive.
        if (_consecutiveBackoffs > 0 && _tickCount - _lastBackoffAt > BackoffResetAfterCleanMs)
        {
            if (_consecutiveBackoffs >= WarnAfterConsecutiveBackoffs)
            {
                logger.Information(
                    "This host has returned idle waits on time for {Duration}ms; idle sleeping is back to normal",
                    BackoffResetAfterCleanMs
                );
            }

            _consecutiveBackoffs = 0;
            _loggedBackoffCeiling = false;
        }

        if (late <= _lateWakeThreshold)
        {
            _consecutiveBadSamples = 0;
            return;
        }

        // Lateness is a rate: an idle loop sleeps hundreds of times a second, so a few outliers are
        // normal, while a host that cannot schedule the process returns most of its waits late. The
        // threshold above is the floor for windows with too few sleeps for a proportion to mean anything.
        if (late * 100 < sleeps * _lateWakePercent)
        {
            _consecutiveBadSamples = 0;
            return;
        }

        // Require persistence: any host can drop one sample to unrelated load, but an oversubscribed
        // one stays bad.
        if (++_consecutiveBadSamples < 2)
        {
            return;
        }

        if (_eventLoopIdleWaitMs <= 0)
        {
            return;
        }

        _currentBackoffMs = Math.Min(BackoffBaseMs << Math.Min(_consecutiveBackoffs, BackoffMaxShift), BackoffMaxMs);
        _consecutiveBackoffs++;
        _lastBackoffAt = _tickCount;
        _idleSleepSuspendedUntil = _tickCount + _currentBackoffMs;
        _idleSleepBackoffs++;

        if (_currentBackoffMs >= BackoffMaxMs)
        {
            // Escalation has run out of room; say so once.
            if (!_loggedBackoffCeiling)
            {
                _loggedBackoffCeiling = true;
                logger.Error(
                    "This host keeps returning idle waits late and sleeping has backed off {Count} times. " +
                    "The process is not being scheduled promptly, which is typical of shared or burstable vCPUs. " +
                    "Set server.eventLoopIdleWaitMs to 0 to disable sleeping permanently and trade a full core for latency.",
                    _idleSleepBackoffs
                );
            }

            return;
        }

        // Each backoff doubles the suspension, so every line is a distinct escalation step and
        // needs no further rate limiting.
        if (_consecutiveBackoffs < WarnAfterConsecutiveBackoffs)
        {
            logger.Debug(
                "This host returned a {Requested}ms idle wait at least {TickRate}ms late {Count} of {Sleeps} time(s) " +
                "in the last second; idle sleeping suspended for {Duration}ms",
                _eventLoopIdleWaitMs,
                Timer.TickRate,
                late,
                sleeps,
                _currentBackoffMs
            );

            return;
        }

        logger.Warning(
            "This host returned a {Requested}ms idle wait at least {TickRate}ms late {Count} of {Sleeps} time(s) in " +
            "the last second, for the {Backoffs}th time running; idle sleeping suspended for {Duration}ms",
            _eventLoopIdleWaitMs,
            Timer.TickRate,
            late,
            sleeps,
            _consecutiveBackoffs,
            _currentBackoffMs
        );
    }
    private static bool _crashed;
    private static string _baseDirectory;

    // Bounds how long an off-loop exit waits for the loop to finish shutting down on its own thread, and
    // how long an off-loop crash waits for the loop to pick up its handling. Windows allows a
    // console-close handler about five seconds before it kills the process anyway.
    private static readonly TimeSpan OffLoopShutdownTimeout = TimeSpan.FromSeconds(5);

    // Bounds the wait for an in-flight save before a crash handled off the loop exits. Not unbounded:
    // the thread that faulted may be the writer.
    private static readonly TimeSpan CrashWriteCompletionTimeout = TimeSpan.FromSeconds(30);

    private static volatile LoopLifecycle _loopLifecycle;
    private static readonly ManualResetEventSlim _shutdownComplete = new(false);

    // Exactly one thread owns the process exit: the loop's DoKill, ProcessExit for an exit already under
    // way, or the thread handling a crash. Only the owner calls Environment.Exit, so the exit code and a
    // restart are decided once.
    private static int _exitClaimed;

    // Kept alive for the life of the process: a collected registration silently unregisters.
    private static PosixSignalRegistration _sigtermRegistration;
    private static int _terminateSignals;

    // A SIGTERM'd server that has not finished shutting down by then is stuck; supervisors otherwise wait
    // for their own kill timeout (systemd: 90 s).
    private static readonly TimeSpan TerminateGracePeriod = TimeSpan.FromSeconds(30);

    // 128 + SIGTERM, what the runtime's default handling would have exited with.
    private const int TerminatedExitCode = 143;

    private static bool? _isRunningFromXUnit;

    private static int _itemCount;
    private static int _mobileCount;
    public static EventLoopContext LoopContext { get; set; }

    private static readonly Type[] _serialTypeArray = { typeof(Serial) };

    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static readonly bool IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static readonly bool IsFreeBSD = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || IsFreeBSD;
    public static readonly bool IsBSD = IsDarwin || IsFreeBSD;
    public static readonly bool Unix = IsBSD || IsLinux;

    private const string AssembliesConfiguration = "Data/assemblies.json";

#nullable enable
    // TODO: Find a way to get rid of this
    public static bool IsRunningFromXUnit
    {
        get
        {
            if (_isRunningFromXUnit != null)
            {
                return _isRunningFromXUnit.Value;
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.FullName.InsensitiveStartsWith("xunit"))
                {
                    _isRunningFromXUnit = true;
                    return true;
                }
            }

            _isRunningFromXUnit = false;
            return false;
        }
    }
#nullable restore

    public static Assembly ApplicationAssembly { get; set; }
    public static Assembly Assembly { get; set; }

    // Assembly file version
    public static Version Version => new(ThisAssembly.AssemblyFileVersion);

    public static Process Process { get; private set; }

    public static Thread Thread { get; private set; }

    private static long _firstTick;

    // Make these available to unit tests for mocking
    internal static long _tickCount;
    internal static DateTime _now;

    public static long TickCount => _tickCount;

    public static DateTime Now => _now;

    public static long Uptime => TickCount - _firstTick;

    public static string BaseDirectory
    {
        get
        {
            if (_baseDirectory == null)
            {
                try
                {
                    _baseDirectory = ApplicationAssembly.Location;

                    if (_baseDirectory.Length > 0)
                    {
                        _baseDirectory = Path.GetDirectoryName(_baseDirectory);
                    }
                }
                catch
                {
                    _baseDirectory = "";
                }
            }

            return _baseDirectory;
        }
    }

    public static CancellationTokenSource ClosingTokenSource { get; } = new();

    public static bool Closing => ClosingTokenSource.IsCancellationRequested;

    public static bool Headless { get; private set; }

    public static int GlobalUpdateRange { get; set; } = 18;

    public static int GlobalMaxUpdateRange { get; set; } = 24;

    public static int ScriptItems => _itemCount;
    public static int ScriptMobiles => _mobileCount;

    public static Expansion Expansion { get; set; }
    public static bool T2A => Expansion >= Expansion.T2A;

    public static bool UOR => Expansion >= Expansion.UOR;

    public static bool UOTD => Expansion >= Expansion.UOTD;

    public static bool LBR => Expansion >= Expansion.LBR;

    public static bool AOS => Expansion >= Expansion.AOS;

    public static bool SE => Expansion >= Expansion.SE;

    public static bool ML => Expansion >= Expansion.ML;

    public static bool SA => Expansion >= Expansion.SA;

    public static bool HS => Expansion >= Expansion.HS;

    public static bool TOL => Expansion >= Expansion.TOL;

    public static bool EJ => Expansion >= Expansion.EJ;

    public static string FindDataFile(string path, bool throwNotFound = true)
    {
        string fullPath = null;

        foreach (var p in ServerConfiguration.DataDirectories)
        {
            fullPath = Path.Combine(p, path);

            if (IsLinux && !File.Exists(fullPath))
            {
                var fi = new FileInfo(fullPath);
                if (fi.Directory != null && Directory.Exists(fi.Directory.FullName))
                {
                    fullPath = fi.Directory.EnumerateFiles(
                        fi.Name,
                        new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }
                    ).FirstOrDefault()?.FullName;
                }
            }

            if (File.Exists(fullPath))
            {
                break;
            }

            fullPath = null;
        }

        if (fullPath == null && throwNotFound)
        {
            throw new FileNotFoundException($"Data: {path} was not found");
        }

        return fullPath;
    }

    public static IEnumerable<string> FindDataFileByPattern(string pattern)
    {
        var options = new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive };
        foreach (var p in ServerConfiguration.DataDirectories)
        {
            if (Directory.Exists(p))
            {
                foreach (var file in Directory.EnumerateFiles(p, pattern, options))
                {
                    yield return file;
                }
            }
        }
    }

    public static void Kill(bool restart = false)
    {
        _restartOnKill = restart;
        _performProcessKill = true;

        // Callers are usually off-loop (console input, signal handlers); wake so the request
        // is noticed now rather than whenever the loop next surfaces.
        NetState.Wake();
    }

    public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e.IsTerminating ? "Error:" : "Warning:");
        Console.WriteLine(e.ExceptionObject);

        if (!e.IsTerminating)
        {
            return;
        }

        var exception = e.ExceptionObject as Exception;
        var onLoopThread = Thread.CurrentThread == Thread;
        var route = ShutdownRouting.RouteCrash(onLoopThread, _loopLifecycle);

        if (route == CrashRoute.RunHere)
        {
            HandleCrash(exception);
            return;
        }

        // Set before claiming the exit, so ProcessExit never mistakes the crash exit for an external one.
        _crashed = true;

        // Another thread is already in Environment.Exit, which may be waiting on this one (the finalizer
        // raises ProcessExit), so never block here.
        if (!TryClaimExit())
        {
            return;
        }

        if (route == CrashRoute.HandOffToLoop && HandOffCrash(exception, out var close))
        {
            AwaitCrashAcknowledgement(close);
            TerminateProcess(_restartOnKill);
            return;
        }

        HandleCrashOffLoop(exception);
    }

    private static void HandleCrash(Exception exception)
    {
        _crashed = true;
        AwaitCrashAcknowledgement(InvokeServerCrashed(exception));
        DoKill(_restartOnKill);
    }

    /// <summary>
    /// Runs the crash subscribers and HandleClosed on the loop and blocks the faulting thread until they
    /// are done; the faulting thread then exits the process. The loop must not call Environment.Exit
    /// itself: when the finalizer faulted, Exit would wait forever for it to raise ProcessExit.
    /// Returns false when the loop did not pick the work up in time.
    /// </summary>
    private static bool HandOffCrash(Exception exception, out bool close)
    {
        var handoff = new CrashHandoff();

        LoopContext.Post(
            () =>
            {
                if (!handoff.TryClaim())
                {
                    return;
                }

                handoff.Started.Set();

                try
                {
                    handoff.Close = InvokeServerCrashed(exception);
                    HandleClosed();
                }
                finally
                {
                    handoff.Finished.Set();
                }

                // The network is shut down and the faulting thread owns the exit; another iteration would
                // run against closed state.
                Thread.Sleep(Timeout.Infinite);
            },
            EventLoopContext.Priority.High
        );

        // The loop may be blocked on the very thread that faulted (a save writer, for example).
        if (!handoff.Started.Wait(OffLoopShutdownTimeout) && handoff.TryClaim())
        {
            logger.Error(
                "The game loop did not take over crash handling within {Timeout}s; handling it off the loop",
                OffLoopShutdownTimeout.TotalSeconds
            );
            close = false;
            return false;
        }

        handoff.Finished.Wait();
        close = handoff.Close;
        return true;
    }

    /// <summary>
    /// Crash handling for when the loop cannot take it. Subscribers see they are off the loop thread and
    /// leave game and network state alone; HandleClosed is skipped for the same reason. The caller has
    /// claimed the exit.
    /// </summary>
    private static void HandleCrashOffLoop(Exception exception)
    {
        AwaitCrashAcknowledgement(InvokeServerCrashed(exception));

        if (!World.WaitForWriteCompletion(CrashWriteCompletionTimeout))
        {
            logger.Error(
                "A world save was still writing {Timeout}s after the crash",
                CrashWriteCompletionTimeout.TotalSeconds
            );
        }

        // A loop that has stopped iterating is in HandleClosed; it parks once it reaches the exit.
        if (_loopLifecycle == LoopLifecycle.ShuttingDown)
        {
            _shutdownComplete.Wait(OffLoopShutdownTimeout);
        }

        TerminateProcess(_restartOnKill);
    }

    private static bool InvokeServerCrashed(Exception exception)
    {
        try
        {
            var args = new ServerCrashedEventArgs(exception);
            EventSink.InvokeServerCrashed(args);
            return args.Close;
        }
        catch
        {
            return false;
        }
    }

    private static void AwaitCrashAcknowledgement(bool close)
    {
        if (!close && !Headless)
        {
            Console.WriteLine("This exception is fatal, press return to exit");
            ConsoleInputHandler.ReadLine();
        }
    }

    private sealed class CrashHandoff
    {
        private int _claimed;

        public readonly ManualResetEventSlim Started = new(false);
        public readonly ManualResetEventSlim Finished = new(false);
        public bool Close;

        // Exactly one of the loop and the faulting thread handles the crash.
        public bool TryClaim() => Interlocked.Exchange(ref _claimed, 1) == 0;
    }

    // Raised on the finalizer thread for every exit, the loop's own Environment.Exit included.
    private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        // After a crash the crash handling owns the exit. Otherwise this exit is not the loop's: claiming
        // it parks the loop at its own exit rather than letting it override this exit code with 0.
        if (Closing || _crashed || !TryClaimExit())
        {
            return;
        }

        if (ShutdownRouting.RouteProcessExit(_loopLifecycle) == ProcessExitRoute.RunHere)
        {
            HandleClosed();
            return;
        }

        // The shutdown handlers touch network and game state, so the loop runs them. This thread only
        // has to keep the process alive until it has: returning lets the runtime finish exiting.
        Kill();

        if (!_shutdownComplete.Wait(OffLoopShutdownTimeout))
        {
            logger.Warning(
                "The process is exiting and the game loop did not shut down within {Timeout}s",
                OffLoopShutdownTimeout.TotalSeconds
            );
        }
    }

    private static void OnTerminateSignal(PosixSignalContext context)
    {
        var priorSignals = Interlocked.Increment(ref _terminateSignals) - 1;
        if (!ShutdownRouting.ShouldInterceptTerminate(_loopLifecycle, priorSignals))
        {
            if (priorSignals > 0)
            {
                logger.Warning("Detected {Signal} again; exiting without waiting for the game loop", context.Signal);
            }

            return;
        }

        logger.Information("Detected {Signal}.", context.Signal);

        // Without the cancel the runtime starts exiting while the loop keeps running.
        context.Cancel = true;
        Kill();

        ThreadPool.UnsafeQueueUserWorkItem(static _ => ExitIfShutdownHangs(), null);
    }

    private static void ExitIfShutdownHangs()
    {
        if (_shutdownComplete.Wait(TerminateGracePeriod))
        {
            return;
        }

        logger.Error(
            "The game loop did not shut down within {Timeout}s of SIGTERM; exiting without it",
            TerminateGracePeriod.TotalSeconds
        );

        // ProcessExit still gives the loop its bounded chance at HandleClosed.
        Environment.Exit(TerminatedExitCode);
    }

    private static void Console_CancelKeyPressed(object sender, ConsoleCancelEventArgs e)
    {
        var keypress = e.SpecialKey switch
        {
            ConsoleSpecialKey.ControlBreak => "CTRL+BREAK",
            _                              => "CTRL+C"
        };

        logger.Information("Detected {Key} pressed.", keypress);
        e.Cancel = true;
        Kill();
    }

    internal static void DoKill(bool restart = false)
    {
        if (Closing)
        {
            return;
        }

        HandleClosed();
        ExitProcess(restart);
    }

    private static bool TryClaimExit() => Interlocked.Exchange(ref _exitClaimed, 1) == 0;

    private static void ExitProcess(bool restart)
    {
        if (TryClaimExit())
        {
            TerminateProcess(restart);
            return;
        }

        // The owner is in, or headed for, Environment.Exit, which ends the process without this thread.
        // Only the loop parks: Exit waits on the finalizer thread, which may be the caller here.
        if (Thread.CurrentThread == Thread)
        {
            Thread.Sleep(Timeout.Infinite);
        }
    }

    private static void TerminateProcess(bool restart)
    {
        if (restart && _restartExitCode != 0)
        {
            logger.Information("Restarting (exiting with code {ExitCode} for the supervisor)", _restartExitCode);
            Environment.Exit(_restartExitCode);
        }

        if (restart)
        {
            try
            {
                logger.Information("Restarting");

                // Relaunch through whichever host started us: the apphost (ModernUO.exe) runs directly,
                // `dotnet ModernUO.dll` needs the assembly path. ArgumentList quotes paths with spaces.
                var hostPath = Environment.ProcessPath ?? "dotnet";
                var startInfo = new ProcessStartInfo(hostPath) { UseShellExecute = false };
                if (Path.GetFileNameWithoutExtension(hostPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.ArgumentList.Add(ApplicationAssembly.Location);
                }

                using var process = Process.Start(startInfo);
                logger.Information("Restart done");
            }
            catch (Exception e)
            {
                logger.Error(e, "Restart failed");
            }
        }

        Environment.Exit(0);
    }

    private static void HandleClosed()
    {
        ClosingTokenSource.Cancel();

        try
        {
            logger.Information("Shutting down");

            World.WaitForWriteCompletion();
            World.ExitSerializationThreads();
            PingServer.Shutdown();
            NetState.Shutdown();
            BanChannel.Stop();
            ConnectionFilters.Stop();

            if (!_crashed)
            {
                EventSink.InvokeShutdown();
            }

            // Nothing compresses after the shutdown subscribers. Off the loop the loop may still be
            // compressing, so the instance is left for the process exit to reclaim.
            if (Thread.CurrentThread == Thread)
            {
                Deflate.DisposeStandard();
            }
        }
        finally
        {
            _shutdownComplete.Set();
        }
    }

    private static readonly bool UseFastTimestampMath = Stopwatch.Frequency % 1000 == 0;
    private static readonly ulong FrequencyInMilliseconds = (ulong)Stopwatch.Frequency / 1000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetTimestamp()
    {
        if (UseFastTimestampMath)
        {
            return (long)((ulong)Stopwatch.GetTimestamp() / FrequencyInMilliseconds);
        }

        // Fast calculation will be lossy, fallback to slower but accurate calculation
        return (long)((UInt128)Stopwatch.GetTimestamp() * 1000 / (ulong)Stopwatch.Frequency);
    }

    public static void Setup(Assembly applicationAssembly, Process process)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        Process = process;
        ApplicationAssembly = applicationAssembly;
        Assembly = Assembly.GetAssembly(typeof(Core));
        Thread = Thread.CurrentThread;
        LoopContext = new EventLoopContext();
        SynchronizationContext.SetSynchronizationContext(LoopContext);

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyHandler.AssemblyResolver;

        Console.OutputEncoding = Encoding.UTF8;
        Thread.Name = "Core Thread";

        if (BaseDirectory.Length > 0)
        {
            Directory.SetCurrentDirectory(BaseDirectory);
        }

        Utility.PushColor(ConsoleColor.Green);
        Console.WriteLine(
            "ModernUO - [https://github.com/modernuo/modernuo] Version {0}.{1}.{2}.{3}",
            Version.Major,
            Version.Minor,
            Version.Build,
            Version.Revision
        );
        Utility.PopColor();

        Utility.PushColor(ConsoleColor.DarkGray);
        Console.WriteLine(@"Copyright 2019-2026 ModernUO Development Team
                This program comes with ABSOLUTELY NO WARRANTY;
                This is free software, and you are welcome to redistribute it under certain conditions.

                You should have received a copy of the GNU General Public License
                along with this program. If not, see <https://www.gnu.org/licenses/>.
            ".TrimMultiline());
        Utility.PopColor();

        Console.CancelKeyPress += Console_CancelKeyPressed;

        // SIGINT and SIGQUIT already arrive through CancelKeyPress. SIGTERM (docker stop, systemctl stop)
        // would otherwise run the shutdown handlers on the runtime's exit thread, concurrently with the loop.
        if (!IsWindows)
        {
            _sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnTerminateSignal);
        }

        Headless = Console.IsInputRedirected;
        if (Headless)
        {
            logger.Information("Headless mode detected (stdin is not a TTY); interactive console input is disabled.");
        }

        // LibDeflate is not thread safe, so each thread gets its own instance; this is the loop's.
        _ = Deflate.Standard;

        ServerConfiguration.Load();

        // 0 disables idle sleeping entirely (full-core spin, zero scheduling overhead).
        var idleWaitMs = ServerConfiguration.GetSetting("server.eventLoopIdleWaitMs", 2);
        if (idleWaitMs < 0)
        {
            logger.Warning(
                "server.eventLoopIdleWaitMs {Value} is negative; using 0 (idle sleeping disabled)",
                idleWaitMs
            );
        }

        _eventLoopIdleWaitMs = Math.Max(0, idleWaitMs);

        // Floor for the backoff: idle waits per second the host may return a full tick late before
        // the rate test below applies at all. Set very high to disable the backoff.
        var lateWakeThreshold = ServerConfiguration.GetSetting("server.lateWakeThreshold", 1);
        if (lateWakeThreshold < 0)
        {
            logger.Warning(
                "server.lateWakeThreshold {Value} is negative; using 0",
                lateWakeThreshold
            );
        }

        _lateWakeThreshold = Math.Max(0, lateWakeThreshold);

        // Share of a second's idle waits that must return late before the backoff trips. 0 leaves
        // the threshold above in sole charge.
        var lateWakePercent = ServerConfiguration.GetSetting("server.lateWakePercent", 10);
        if (lateWakePercent is < 0 or > 100)
        {
            logger.Warning(
                "server.lateWakePercent {Value} is outside 0-100; using {Clamped}",
                lateWakePercent,
                Math.Clamp(lateWakePercent, 0, 100)
            );
        }

        _lateWakePercent = Math.Clamp(lateWakePercent, 0, 100);

        // Unix truncates exit codes to a byte, so anything outside 1-255 cannot be told apart reliably.
        var restartExitCode = ServerConfiguration.GetSetting("server.restartExitCode", 0);
        if (restartExitCode is < 0 or > 255)
        {
            logger.Warning(
                "server.restartExitCode {Value} is outside 1-255; restarts will relaunch the server directly",
                restartExitCode
            );
            restartExitCode = 0;
        }

        _restartExitCode = restartExitCode;

        var assemblyPath = Path.Join(BaseDirectory, AssembliesConfiguration);

        // Load UOContent.dll
        var assemblyFiles = JsonConfig.Deserialize<List<string>>(assemblyPath)?.ToArray();
        if (assemblyFiles == null)
        {
            throw new JsonException($"Failed to deserialize {assemblyPath}.");
        }

        for (var i = 0; i < assemblyFiles.Length; i++)
        {
            assemblyFiles[i] = Path.Join(BaseDirectory, "Assemblies", assemblyFiles[i]);
        }

        AssemblyHandler.LoadAssemblies(assemblyFiles);

        // First-boot interactive setup. After assemblies load so content can register prompts,
        // before any Serilog output so prompts are not interleaved with the async console sink.
        AssemblyHandler.Invoke("ConfigurePrompts");

        logger.Information("Running on {Framework}", RuntimeInformation.FrameworkDescription);

        VerifySerialization();

        _now = DateTime.UtcNow;
        _firstTick = _tickCount = GetTimestamp();

        // Seed from a real tick: tick counts need not start near zero, so a zero-initialized
        // deadline compares wrong. See dev-docs/tick-counts.md.
        _nextHealthSample = _tickCount + HealthSampleIntervalMs;
        _idleSleepSuspendedUntil = _tickCount;

        Timer.Init(_tickCount);

        AssemblyHandler.Invoke("Configure");

        TileMatrixLoader.LoadTileMatrix();

        RegionJsonSerializer.LoadRegions();
        World.Load();

        AssemblyHandler.Invoke("Initialize");

        BanChannel.Start(ClosingTokenSource.Token);
        ConnectionFilters.Start(ClosingTokenSource.Token);
        NetState.Start();
        PingServer.Start();
        EventSink.InvokeServerStarted();

        // Without a high-resolution wait a 2ms request quantises to 15.625ms and the loop runs a
        // tick behind. Only fires when the high-res timer and the timeBeginPeriod fallback both failed.
        if (_eventLoopIdleWaitMs > 0 && NetState.Ring?.SupportsHighResolutionWait == false)
        {
            logger.Error(
                "This host cannot honor short waits (no high-resolution timer, and raising the system timer " +
                "resolution failed). Idle sleeping is disabled. The loop will spin instead, using a full core."
            );

            IdleSleepUnsupported = true;
            _eventLoopIdleWaitMs = 0;
        }

        RunEventLoop();
    }

    /// <summary>
    /// True when every queue the loop drains is empty, so sleeping cannot strand pending work.
    /// The drains are bounded, so leftovers are normal and must keep the loop awake.
    /// </summary>
    private static bool IsIdle() =>
        !Mobile.HasQueuedDeltas && !Item.HasQueuedDeltas && LoopContext.IsEmpty && NetState.IsIdle;

    public static void RunEventLoop()
    {
        _loopLifecycle = LoopLifecycle.Running;

        try
        {
            while (!Closing)
            {
                _tickCount = GetTimestamp();
                _now = DateTime.UtcNow;

                EventLoopProfiler.IterationStart(_tickCount);

                EventLoopProfiler.PhaseStart(LoopPhase.MobileDeltas);
                Mobile.ProcessDeltaQueue();
                EventLoopProfiler.PhaseEnd(LoopPhase.MobileDeltas);

                EventLoopProfiler.PhaseStart(LoopPhase.ItemDeltas);
                Item.ProcessDeltaQueue();
                EventLoopProfiler.PhaseEnd(LoopPhase.ItemDeltas);

                EventLoopProfiler.PhaseStart(LoopPhase.TimerSlice);
                Timer.Slice(_tickCount);
                EventLoopProfiler.PhaseEnd(LoopPhase.TimerSlice);

                // Handle networking
                EventLoopProfiler.PhaseStart(LoopPhase.NetworkSlice);
                NetState.Slice();
                EventLoopProfiler.PhaseEnd(LoopPhase.NetworkSlice);

                // Execute captured post-await methods (like Timer.Pause)
                EventLoopProfiler.PhaseStart(LoopPhase.LoopTasks);
                LoopContext.ExecuteTasks();
                EventLoopProfiler.PhaseEnd(LoopPhase.LoopTasks);

                Timer.CheckTimerPool(); // Check for pool depletion so we can async refill it.

                if (_performSnapshot)
                {
                    EventLoopProfiler.PhaseStart(LoopPhase.WorldSnapshot);
                    // Return value is the offset that can be used to fix timers that should drift
                    World.Snapshot(_snapshotPath);
                    EventLoopProfiler.PhaseEnd(LoopPhase.WorldSnapshot);
                    _performSnapshot = false;
                }

                if (_performProcessKill)
                {
                    World.WaitForWriteCompletion();
                    break;
                }

                CheckSchedulerHealth();

                if (_eventLoopIdleWaitMs > 0 && _tickCount - _idleSleepSuspendedUntil >= 0 && IsIdle())
                {
                    // Re-read the clock: a stale timestamp overstates the time to the next tick
                    // and sleeps straight past it.
                    var start = GetTimestamp();
                    var due = Timer.MillisecondsUntilNextTick(start);
                    if (due > 0)
                    {
                        var requested = (int)Math.Min(due, _eventLoopIdleWaitMs);

                        // The GC prefers to collect during idle sleeps, so its pauses land here by
                        // design and are not the host's fault. Gen1 and above (what
                        // CollectionCount(1) counts) are the only pauses long enough to reach a tick.
                        var collections = GC.CollectionCount(1);

                        NetState.WaitForCompletion(requested);

                        var elapsed = GetTimestamp() - start;
                        EventLoopProfiler.SleepEnd(requested, elapsed);
                        _sleepAttempts++;

                        // The second collection read sits behind the overshoot test, so the common
                        // path reads the counter once, not twice.
                        if (elapsed - requested >= Timer.TickRate && GC.CollectionCount(1) == collections)
                        {
                            _lateWakes++;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            _loopLifecycle = LoopLifecycle.ShuttingDown;
            CurrentDomain_UnhandledException(null, new UnhandledExceptionEventArgs(e, true));
            return;
        }

        _loopLifecycle = LoopLifecycle.ShuttingDown;
        DoKill(_restartOnKill);
    }

    internal static void RequestSnapshot(string snapshotPath)
    {
        _snapshotPath = snapshotPath;
        _performSnapshot = true;

        // Save requests arrive off-loop; wake so the snapshot starts now.
        NetState.Wake();
    }

    public static void VerifySerialization()
    {
        _itemCount = 0;
        _mobileCount = 0;

        var callingAssembly = Assembly.GetCallingAssembly();

        VerifySerialization(callingAssembly);

        foreach (var assembly in AssemblyHandler.Assemblies)
        {
            if (assembly != callingAssembly)
            {
                VerifySerialization(assembly);
            }
        }
    }

    private static void VerifyType(Type type)
    {
        if (!type.IsAssignableTo(typeof(ISerializable)) || type.IsInterface || type.IsAbstract)
        {
            return;
        }

        if (type.IsSubclassOf(typeof(Item)))
        {
            Interlocked.Increment(ref _itemCount);
        }
        else if (type.IsSubclassOf(typeof(Mobile)))
        {
            Interlocked.Increment(ref _mobileCount);
        }

        using var errors = ValueStringBuilder.CreateMT();

        try
        {
            if (World.DirtyTrackingEnabled)
            {
                var manualDirtyCheckingAttribute = type.GetCustomAttribute<ManualDirtyCheckingAttribute>(false);
                var codeGennedAttribute = type.GetCustomAttribute<ModernUO.Serialization.SerializationGeneratorAttribute>(false);

                if (manualDirtyCheckingAttribute == null && codeGennedAttribute == null)
                {
                    errors.AppendLine("       - No property tracking (dirty checking)");
                }
            }

            if (type.GetConstructor(_serialTypeArray) == null)
            {
                errors.AppendLine("       - No serialization constructor");
            }

            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                              BindingFlags.Instance | BindingFlags.DeclaredOnly;

            var hasSerializeMethod = false;
            var hasDeserializeMethod = false;

            foreach (var method in type.GetMethods(bindingFlags))
            {
                if (method.Name == "Serialize")
                {
                    hasSerializeMethod = true;
                }

                if (method.Name == "Deserialize")
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IGenericReader))
                    {
                        hasDeserializeMethod = true;
                    }
                }
            }

            if (!hasSerializeMethod)
            {
                errors.AppendLine("       - No Serialize() method");
            }

            if (!hasDeserializeMethod)
            {
                errors.AppendLine("       - No Deserialize() method");
            }

            if (errors.Length > 0)
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine($"{type}{Environment.NewLine}{errors.ToString()}");
                Utility.PopColor();
            }
        }
        catch (AmbiguousMatchException e)
        {
            // ignored
        }
        catch
        {
            Console.WriteLine("Warning: Exception in serialization verification of type {0}", type);
        }
    }

    private static void VerifySerialization(Assembly assembly)
    {
        if (assembly != null)
        {
            Parallel.ForEach(assembly.GetTypes(), VerifyType);
        }
    }
}
