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

        if (e.IsTerminating)
        {
            _crashed = true;

            var close = false;

            try
            {
                var args = new ServerCrashedEventArgs(e.ExceptionObject as Exception);

                EventSink.InvokeServerCrashed(args);

                close = args.Close;
            }
            catch
            {
                // ignored
            }

            if (!close && !Headless)
            {
                Console.WriteLine("This exception is fatal, press return to exit");
                ConsoleInputHandler.ReadLine();
            }

            DoKill();
        }
    }

    private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        if (!Closing)
        {
            HandleClosed();
        }
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

        if (restart)
        {
            try
            {
                logger.Information("Restarting");
                if (IsWindows)
                {
                    using var process = Process.Start("dotnet", $"{ApplicationAssembly.Location}");
                }
                else
                {
                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"{ApplicationAssembly.Location}",
                        UseShellExecute = true
                    };

                    process.Start();
                }
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

        Headless = Console.IsInputRedirected;
        if (Headless)
        {
            logger.Information("Headless mode detected (stdin is not a TTY); interactive console input is disabled.");
        }

        // LibDeflate is not thread safe, so we need to create a new instance for each thread
        var standard = Deflate.Standard;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => standard.Dispose();

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
            CurrentDomain_UnhandledException(null, new UnhandledExceptionEventArgs(e, true));
            return;
        }

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
