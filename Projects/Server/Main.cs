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

    // Longest the loop will block when it has nothing to do. This is a backstop, not a latency
    // control: because the timer wheel's tick rate bounds the sleep anyway, its only job is to
    // limit the damage if a wake signal is ever missed. 0 selects the legacy spin loop, which
    // exists so the two can be measured against each other on the same host.
    private static int _eventLoopIdleWaitMs = 2;

    /// <summary>
    /// Longest the loop will block while idle, in milliseconds. 0 means the legacy spin loop.
    /// </summary>
    public static int EventLoopIdleWaitMs => _eventLoopIdleWaitMs;

    // Loop-thread only, so plain increments are safe.
    private static long _loopIterations;
    private static long _loopSleeps;

    /// <summary>
    /// Loop iterations since the last <see cref="ResetLoopCounters"/>.
    /// </summary>
    public static long LoopIterations => _loopIterations;

    /// <summary>
    /// Iterations that actually blocked, since the last <see cref="ResetLoopCounters"/>.
    /// </summary>
    /// <remarks>
    /// The ratio of this to <see cref="LoopIterations"/> is how to tell whether idle sleeping is
    /// doing anything on a given shard. Under sustained load it should approach zero, because the
    /// queues are never all empty -- which also means the wake signal is not being exercised, and
    /// any cost it carries is pure overhead there.
    /// </remarks>
    public static long LoopSleeps => _loopSleeps;

    public static void ResetLoopCounters()
    {
        _loopIterations = 0;
        _loopSleeps = 0;
    }

    // Adaptive backoff. Idle sleeping is the right default, but it can only ever make latency
    // worse in one way: the wait may return late, and a late return means the wheel loses a slot
    // that a spinning loop would have caught. Rather than trade that risk off blindly, watch for
    // it and stop sleeping while it is happening.
    private const long BackoffSampleIntervalMs = 1000;
    private const long BackoffDurationMs = 5000;

    private static long _nextBackoffSample;
    private static long _skippedTicksAtLastSample;
    private static long _idleSleepSuspendedUntil;
    private static int _skippedTickThreshold = 2;
    private static long _idleSleepBackoffs;
    private static bool _backoffPrimed;
    private static int _consecutiveBadSamples;

    /// <summary>
    /// Times the loop has stopped sleeping because the wheel was losing slots.
    /// </summary>
    public static long IdleSleepBackoffs => _idleSleepBackoffs;

    /// <summary>
    /// Whether idle sleeping is currently suspended by the adaptive backoff.
    /// </summary>
    public static bool IdleSleepSuspended => _tickCount < _idleSleepSuspendedUntil;

    /// <summary>
    /// Samples how many wheel slots were lost since the last check and suspends idle sleeping if
    /// too many were, so a struggling shard spins rather than risking a late wake.
    /// </summary>
    /// <remarks>
    /// Measured on an idle shard, both the spin loop and 2ms sleeping lose about one slot in
    /// 7,500, which is the operating system rather than the scheduler. The threshold is set well
    /// above that floor so ordinary jitter does not trip it, and the suspension is held for
    /// several seconds so the loop is not flapping between modes on a single hiccup.
    /// </remarks>
    private static void SampleTickHealth()
    {
        if (_tickCount < _nextBackoffSample)
        {
            return;
        }

        _nextBackoffSample = _tickCount + BackoffSampleIntervalMs;

        var skipped = Timer.SkippedTicks;
        var lost = skipped - _skippedTicksAtLastSample;
        _skippedTicksAtLastSample = skipped;

        // The wheel is initialised before the world loads, so the loop's very first Slice turns it
        // once for every 8ms that loading took -- hundreds of slots, none of them a stall. Prime
        // the baseline off that first sample instead of judging it.
        if (!_backoffPrimed)
        {
            _backoffPrimed = true;
            return;
        }

        if (lost <= _skippedTickThreshold)
        {
            _consecutiveBadSamples = 0;
            return;
        }

        // Require the condition to persist. Startup loses slots legitimately while tiered JIT and
        // first-touch initialisation settle, and any host can drop one sample to unrelated load;
        // neither is a reason to abandon idle sleeping. A shard that is genuinely behind stays
        // behind, so it trips on the second sample instead.
        if (++_consecutiveBadSamples < 2)
        {
            return;
        }

        // Already suspended: extend rather than counting it as a fresh backoff, so the counter
        // reflects distinct episodes instead of how long one lasted.
        var wasSuspended = _tickCount < _idleSleepSuspendedUntil;
        _idleSleepSuspendedUntil = _tickCount + BackoffDurationMs;

        if (!wasSuspended)
        {
            _idleSleepBackoffs++;
            logger.Warning(
                "Event loop is losing timer slots ({Lost} in {Interval}ms); idle sleeping suspended for {Duration}ms",
                lost,
                BackoffSampleIntervalMs,
                BackoffDurationMs
            );
        }
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

    private static double _currentCPS;
    private static double _averageCPS;
    private static bool _cpsInitialized;

    public static double CyclesPerSecond => _currentCPS;

    public static double AverageCPS => _averageCPS;

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

        // Read before the loop starts. 0 restores the pre-2026 spin loop, which is retained so a
        // shard can measure the two schedulers against each other on identical hardware.
        _eventLoopIdleWaitMs = ServerConfiguration.GetOrUpdateSetting("server.eventLoopIdleWaitMs", 2);

        // Wheel slots lost per second before idle sleeping backs off. An idle shard loses roughly
        // one slot in 7,500 turns (~125/sec) whether it sleeps or spins, so this sits well above
        // the noise floor. Raise it to tolerate a jittery host, or set it very high to disable the
        // backoff entirely.
        _skippedTickThreshold = ServerConfiguration.GetOrUpdateSetting("server.skippedTickThreshold", 2);

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
        RunEventLoop();
    }

    /// <summary>
    /// True when every queue the loop drains is empty, so sleeping cannot strand pending work.
    /// </summary>
    /// <remarks>
    /// This is what lets the loop use a whole core when it needs one: under load these are
    /// non-empty, the loop never sleeps, and it runs flat out. It is not merely a timer check
    /// because the drains above are deliberately bounded -- ProcessDeltaQueue stops at the count
    /// it saw on entry, ExecuteTasks stops at its per-frame cap -- so leftovers are normal and
    /// must keep the loop awake.
    /// </remarks>
    private static bool IsIdle() =>
        !Mobile.HasQueuedDeltas && !Item.HasQueuedDeltas && LoopContext.IsEmpty && NetState.IsIdle;

    public static void RunEventLoop()
    {
        try
        {
            var lastRaw = Stopwatch.GetTimestamp();
            const int interval = 100;
            double frequency = Stopwatch.Frequency * interval;
            const double alpha = 2.0 / 129; // EMA smoothing (≈128-sample window)

            var sample = 0;

            while (!Closing)
            {
                _tickCount = GetTimestamp();
                _now = DateTime.UtcNow;

                Mobile.ProcessDeltaQueue();
                Item.ProcessDeltaQueue();
                Timer.Slice(_tickCount);

                // Handle networking
                NetState.Slice();

                // Execute captured post-await methods (like Timer.Pause)
                LoopContext.ExecuteTasks();

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

                if (sample++ == interval)
                {
                    sample = 0;
                    var nowRaw = Stopwatch.GetTimestamp();

                    _currentCPS = frequency / (nowRaw - lastRaw);

                    if (!_cpsInitialized)
                    {
                        _averageCPS = _currentCPS;
                        _cpsInitialized = true;
                    }
                    else
                    {
                        _averageCPS += alpha * (_currentCPS - _averageCPS);
                    }

                    lastRaw = nowRaw;

                    if (_eventLoopIdleWaitMs <= 0)
                    {
                        // Legacy scheduling, kept so the new behaviour can be A/B measured against
                        // it on the same host. Only considers sleeping once per `interval`
                        // iterations, and spins through the loop body the rest of the time.
                        var sleepMs = (int)Timer.MillisecondsUntilNextTick(_tickCount);
                        if (sleepMs >= 2)
                        {
                            NetState.WaitForCompletion(sleepMs - 1);
                        }
                    }
                }

                _loopIterations++;
                SampleTickHealth();

                if (_eventLoopIdleWaitMs > 0 && _tickCount >= _idleSleepSuspendedUntil && IsIdle())
                {
                    // Re-read the clock rather than reusing _tickCount: the loop body above has
                    // consumed real time, and a stale timestamp would overstate how long is left
                    // before the next tick, so we would sleep straight past it and manufacture
                    // the very tick lag this is meant to remove.
                    var due = Timer.MillisecondsUntilNextTick(GetTimestamp());
                    if (due > 0)
                    {
                        _loopSleeps++;
                        NetState.WaitForCompletion((int)Math.Min(due, _eventLoopIdleWaitMs));
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
