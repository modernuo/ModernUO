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

    // Written from other threads (Kill, RequestSnapshot) and read by the event loop. Volatile
    // because the loop now genuinely blocks between reads rather than spinning past them.
    private static volatile bool _performProcessKill;
    private static bool _restartOnKill;
    private static volatile bool _performSnapshot;
    private static string _snapshotPath;

    // A backstop, not a latency control: the wheel's tick rate bounds the sleep, so this only
    // limits the damage if a wake signal is ever missed. Measured across 1/2/4/8ms, 2 is optimal.
    private static int _eventLoopIdleWaitMs = 2;

    /// <summary>
    /// Longest the loop will block while idle, in milliseconds. 0 disables idle sleeping,
    /// leaving the loop to spin; the adaptive backoff does the same thing temporarily when the
    /// host keeps returning waits late.
    /// </summary>
    public static int EventLoopIdleWaitMs => _eventLoopIdleWaitMs;

    /// <summary>
    /// Whether idle sleeping is currently suspended because the host returned waits late.
    /// </summary>
    /// <remarks>
    /// Compared by subtraction, never directly: tick counts can start enormous and wrap.
    /// See dev-docs/tick-counts.md.
    /// </remarks>
    public static bool IdleSleepSuspended => _tickCount - _idleSleepSuspendedUntil < 0;

    private const long HealthSampleIntervalMs = 1000;

    // Backoff escalates by doubling: a fixed suspension oscillates forever on a persistently bad
    // host, while doubling converges on "stop sleeping" within minutes yet still recovers from a
    // transient problem.
    private const long BackoffBaseMs = 5000;
    private const long BackoffMaxMs = 120_000;
    private const int BackoffMaxShift = 5;

    // Clean streak that clears the escalation.
    private const long BackoffResetAfterCleanMs = 60_000;

    // A sleep is bounded by the time to the next wheel turn, so a correctly honoured sleep can
    // never miss a deadline; the only way sleeping harms the wheel is the wait returning late
    // (the host descheduled the process). That overshoot is measured per sleep, which is why
    // server work -- saves, heavy commands, deep timer callbacks -- cannot trip this backoff.
    // Loop-thread only, so plain increments are safe.
    private static int _lateWakes;

    private static long _nextHealthSample;
    private static long _idleSleepSuspendedUntil;
    private static int _lateWakeThreshold = 1;
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
        _lateWakes = 0;

        if (late <= _lateWakeThreshold)
        {
            _consecutiveBadSamples = 0;
            return;
        }

        // Require the condition to persist: any host can drop one sample to unrelated load, and a
        // host that is genuinely oversubscribed stays that way, so it trips on the second sample.
        if (++_consecutiveBadSamples < 2)
        {
            return;
        }

        if (_eventLoopIdleWaitMs <= 0)
        {
            return;
        }

        // Already suspended: extend rather than counting a fresh backoff episode.
        if (_tickCount - _idleSleepSuspendedUntil < 0)
        {
            _idleSleepSuspendedUntil = _tickCount + _currentBackoffMs;
            return;
        }

        // A long clean streak resets the escalation. Gated on the count rather than a
        // "_lastBackoffAt > 0" sentinel because tick counts are not guaranteed positive.
        if (_consecutiveBackoffs > 0 && _tickCount - _lastBackoffAt > BackoffResetAfterCleanMs)
        {
            _consecutiveBackoffs = 0;
        }

        _currentBackoffMs = Math.Min(BackoffBaseMs << Math.Min(_consecutiveBackoffs, BackoffMaxShift), BackoffMaxMs);
        _consecutiveBackoffs++;
        _lastBackoffAt = _tickCount;
        _idleSleepSuspendedUntil = _tickCount + _currentBackoffMs;
        _idleSleepBackoffs++;

        if (_currentBackoffMs >= BackoffMaxMs)
        {
            // Escalation has run out of room; say so once in terms the operator can act on.
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

        logger.Warning(
            "This host returned a {Requested}ms idle wait at least {TickRate}ms late {Count} time(s) in the last " +
            "second; idle sleeping suspended for {Duration}ms",
            _eventLoopIdleWaitMs,
            Timer.TickRate,
            late,
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

        // Callers are usually off-loop (console input, signal handlers). Without this the loop
        // would not notice the request until it woke for some other reason.
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
        _eventLoopIdleWaitMs = ServerConfiguration.GetOrUpdateSetting("server.eventLoopIdleWaitMs", 2);

        // 16ms-budget misses per second before idle sleeping backs off. Raise to tolerate a
        // jittery host; set very high to disable the backoff.
        _lateWakeThreshold = ServerConfiguration.GetOrUpdateSetting("server.lateWakeThreshold", 1);

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

        // First-boot interactive setup. Runs after assemblies are loaded (so content can
        // register prompts) but before any Serilog output, so console prompts are not
        // interleaved with the async console sink. Handlers self-gate on first-boot state
        // (e.g. "is my setting already present?").
        AssemblyHandler.Invoke("ConfigurePrompts");

        logger.Information("Running on {Framework}", RuntimeInformation.FrameworkDescription);

        VerifySerialization();

        _now = DateTime.UtcNow;
        _firstTick = _tickCount = GetTimestamp();

        // Seed schedule state from the first real tick: tick counts are not guaranteed to start
        // anywhere near zero (hypervisor pass-through counters), so zero-initialized deadlines
        // would compare wrong. See dev-docs/tick-counts.md.
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

        // Without a high-resolution wait a 2ms request quantises to 15.625ms and the loop would
        // quietly run a tick behind; spinning is the lesser evil and must not be silent. Only
        // fires when both the ring's high-res timer and its timeBeginPeriod fallback failed.
        if (_eventLoopIdleWaitMs > 0 && NetState.Ring?.SupportsHighResolutionWait == false)
        {
            logger.Error(
                "This host cannot honour short waits (no high-resolution timer, and raising the system timer " +
                "resolution failed). Idle sleeping is disabled -- the loop will spin instead, using a full core."
            );

            _eventLoopIdleWaitMs = 0;
        }

        RunEventLoop();
    }

    /// <summary>
    /// True when every queue the loop drains is empty, so sleeping cannot strand pending work.
    /// The drains are bounded (ProcessDeltaQueue stops at the count seen on entry, ExecuteTasks
    /// at its per-frame cap), so leftovers are normal and must keep the loop awake.
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
                    // Return value is the offset that can be used to fix timers that should drift
                    World.Snapshot(_snapshotPath);
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
                    // Re-read the clock: the loop body consumed real time, and a stale timestamp
                    // would overstate the time to the next tick and sleep straight past it.
                    var start = GetTimestamp();
                    var due = Timer.MillisecondsUntilNextTick(start);
                    if (due > 0)
                    {
                        var requested = (int)Math.Min(due, _eventLoopIdleWaitMs);
                        NetState.WaitForCompletion(requested);

                        var elapsed = GetTimestamp() - start;
                        EventLoopProfiler.SleepEnd(requested, elapsed);

                        // A sleep is bounded by the next wheel turn, so only a wait the host
                        // returned late can cost the wheel a deadline.
                        if (elapsed - requested >= Timer.TickRate)
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

        // Save requests arrive off-loop. Wake so the snapshot starts now rather than after the
        // loop happens to surface for another reason.
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
