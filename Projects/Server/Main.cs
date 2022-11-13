/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Server.Buffers;
using Server.Json;
using Server.Logging;
using Server.Network;

namespace Server;

public static class Core
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(Core));

    private static bool _crashed;
    private static string _baseDirectory;

    private static bool _profiling;
    private static long _profileStart;
    private static long _profileTime;
#nullable enable
    private static bool? _isRunningFromXUnit;
#nullable restore

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

    public static bool Profiling
    {
        get => _profiling;
        set
        {
            if (_profiling == value)
            {
                return;
            }

            _profiling = value;

            if (_profileStart > 0)
            {
                _profileTime += Stopwatch.GetTimestamp() - _profileStart;
            }

            _profileStart = _profiling ? Stopwatch.GetTimestamp() : 0;
        }
    }

    public static TimeSpan ProfileTime =>
        TimeSpan.FromTicks(_profileStart > 0 ? _profileTime + (Stopwatch.GetTimestamp() - _profileStart) : _profileTime);

    public static Assembly Assembly { get; set; }

    // Assembly file version
    public static Version Version => new(ThisAssembly.AssemblyFileVersion);

    public static Process Process { get; private set; }

    public static Thread Thread { get; private set; }

    [ThreadStatic]
    private static long _tickCount;

    // Don't access this from other threads than the game thread.
    // Persistence accesses this via AfterSerialize or Serialize in other threads, but the value is set and won't change
    // since the game loop is frozen at that moment.
    private static DateTime _now;

    // For Unix Stopwatch.Frequency is normalized to 1ns
    // We don't anticipate needing this for Windows/OSX
    private const long _maxTickCountBeforePrecisionLoss = long.MaxValue / 1000L;
    private static readonly long _ticksPerMillisecond = Stopwatch.Frequency / 1000L;

    public static long TickCount
    {
        get
        {
            if (_tickCount != 0)
            {
                return _tickCount;
            }

            var timestamp = Stopwatch.GetTimestamp();
            return timestamp > _maxTickCountBeforePrecisionLoss
                ? timestamp / _ticksPerMillisecond
                // No precision loss
                : 1000L * timestamp / Stopwatch.Frequency;
        }
        // Setting this to a value lower than the previous is bad. Timers will become delayed
        // until time catches up.
        set => _tickCount = value;
    }

    public static DateTime Now
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // See notes above for _now and why this is a volatile variable.
            var now = _now;
            return now == DateTime.MinValue ? DateTime.UtcNow : now;
        }
    }

    private static long _cycleIndex = 1;
    private static float[] _cyclesPerSecond = new float[128];

    public static float CyclesPerSecond => _cyclesPerSecond[(_cycleIndex - 1) % _cyclesPerSecond.Length];

    public static bool MultiProcessor { get; private set; }

    public static int ProcessorCount { get; private set; }

    public static string BaseDirectory
    {
        get
        {
            if (_baseDirectory == null)
            {
                try
                {
                    _baseDirectory = Assembly.Location;

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

    public static string Arguments
    {
        get
        {
            var sb = new StringBuilder();

            if (_profiling)
            {
                Utility.Separate(sb, "-profile", " ");
            }

            return sb.ToString();
        }
    }

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

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
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

            if (!close)
            {
                Console.WriteLine("This exception is fatal, press return to exit");
                Console.ReadLine();
            }

            Kill();
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

    public static void Kill(bool restart = false)
    {
        if (Closing)
        {
            return;
        }

        HandleClosed();

        if (restart)
        {
            if (IsWindows)
            {
                Process.Start("dotnet", Assembly.Location);
            }
            else
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = Assembly.Location,
                        UseShellExecute = true
                    }
                };

                process.Start();
            }
        }

        Process.Kill();
    }

    private static void HandleClosed()
    {
        ClosingTokenSource.Cancel();

        logger.Information("Shutting down");

        World.WaitForWriteCompletion();

        if (!_crashed)
        {
            EventSink.InvokeShutdown();
        }
    }

    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        AppDomain.CurrentDomain.AssemblyResolve += AssemblyHandler.AssemblyResolver;

        LoopContext = new EventLoopContext();

        SynchronizationContext.SetSynchronizationContext(LoopContext);

        foreach (var a in args)
        {
            if (a.InsensitiveEquals("-profile"))
            {
                Profiling = true;
            }
        }

        Thread = Thread.CurrentThread;
        Process = Process.GetCurrentProcess();
        Assembly = Assembly.GetEntryAssembly();

        if (Assembly == null)
        {
            throw new Exception("Assembly entry is missing.");
        }

        if (Thread != null)
        {
            Thread.Name = "Core Thread";
        }

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
        Console.WriteLine(@"Copyright 2019-2022 ModernUO Development Team
                This program comes with ABSOLUTELY NO WARRANTY;
                This is free software, and you are welcome to redistribute it under certain conditions.

                You should have received a copy of the GNU General Public License
                along with this program. If not, see <https://www.gnu.org/licenses/>.
            ".TrimMultiline());
        Utility.PopColor();

        ProcessorCount = Environment.ProcessorCount;

        if (ProcessorCount > 1)
        {
            MultiProcessor = true;
        }

        Console.CancelKeyPress += Console_CancelKeyPressed;

        ServerConfiguration.Load();

        logger.Information("Running on {Framework}", RuntimeInformation.FrameworkDescription);

        var s = Arguments;

        if (s.Length > 0)
        {
            logger.Information("Running with arguments: {Args}", s);
        }

        if (MultiProcessor)
        {
            logger.Information($"Optimizing for {{ProcessorCount}} processor{(ProcessorCount == 1 ? "" : "s")}", ProcessorCount);
        }

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

        VerifySerialization();

        Timer.Init(TickCount);

        AssemblyHandler.Invoke("Configure");

        TileMatrixLoader.LoadTileMatrix();

        RegionLoader.LoadRegions();
        World.Load();

        AssemblyHandler.Invoke("Initialize");

        TcpServer.Start();
        EventSink.InvokeServerStarted();
        RunEventLoop();
    }

    public static void RunEventLoop()
    {
        try
        {
#if DEBUG
            const bool idleCPU = true;
#else
                var idleCPU = ServerConfiguration.GetOrUpdateSetting("core.enableIdleCPU", false);
#endif

            long last = TickCount;
            const int interval = 100;
            const float ticksPerSecond = 1000 * interval;

            int sample = 0;

            while (!Closing)
            {
                _tickCount = TickCount;
                _now = DateTime.UtcNow;

                Mobile.ProcessDeltaQueue();
                Item.ProcessDeltaQueue();
                Timer.Slice(_tickCount);

                // Handle networking
                TcpServer.Slice();
                NetState.Slice();

                // Execute captured post-await methods (like Timer.Pause)
                LoopContext.ExecuteTasks();

                Timer.CheckTimerPool(); // Check for pool depletion so we can async refill it.

                _tickCount = 0;
                _now = DateTime.MinValue;

                if (idleCPU && ++sample % interval == 0)
                {
                    var now = TickCount;

                    var cyclesPerSecond = ticksPerSecond / (now - last);
                    _cyclesPerSecond[_cycleIndex++ % _cyclesPerSecond.Length] = cyclesPerSecond;
                    last = now;

                    if (cyclesPerSecond > 80)
                    {
                        Thread.Sleep(2);
                    }
                }
            }
        }
        catch (Exception e)
        {
            CurrentDomain_UnhandledException(null, new UnhandledExceptionEventArgs(e, true));
        }
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
