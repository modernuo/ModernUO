/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Json;
using Server.Network;

namespace Server
{
    public static class Core
    {
        private static bool m_Crashed;
        private static Thread timerThread;
        private static string m_BaseDirectory;

        private static bool m_Profiling;
        private static DateTime m_ProfileStart;
        private static TimeSpan m_ProfileTime;
        private static bool? m_IsRunningFromXUnit;

        private static int m_CycleIndex = 1;
        private static readonly float[] m_CyclesPerSecond = new float[100];

        // private static readonly AutoResetEvent m_Signal = new AutoResetEvent(true);

        private static int m_ItemCount;
        private static int m_MobileCount;

        private static readonly Type[] m_SerialTypeArray = { typeof(Serial) };

        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static readonly bool IsFreeBSD = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || IsFreeBSD;
        public static readonly bool Unix = IsDarwin || IsFreeBSD || IsLinux;

        private const string assembliesConfiguration = "Data/assemblies.json";

        public static bool IsRunningFromXUnit =>
            m_IsRunningFromXUnit ??= AppDomain.CurrentDomain.GetAssemblies()
                .Any(
                    a => a.FullName?.ToLowerInvariant().StartsWith("xunit") ?? false
                );

        public static bool Profiling
        {
            get => m_Profiling;
            set
            {
                if (m_Profiling == value)
                {
                    return;
                }

                m_Profiling = value;

                if (m_ProfileStart > DateTime.MinValue)
                {
                    m_ProfileTime += DateTime.UtcNow - m_ProfileStart;
                }

                m_ProfileStart = m_Profiling ? DateTime.UtcNow : DateTime.MinValue;
            }
        }

        public static TimeSpan ProfileTime =>
            m_ProfileStart > DateTime.MinValue
                ? m_ProfileTime + (DateTime.UtcNow - m_ProfileStart)
                : m_ProfileTime;

        public static Assembly Assembly { get; set; }

        // Assembly file version
        public static Version Version => Version.Parse(
            FileVersionInfo.GetVersionInfo(Assembly.Location).FileVersion
        );

        public static Process Process { get; private set; }

        public static Thread Thread { get; private set; }

        public static long TickCount => 1000L * Stopwatch.GetTimestamp() / Stopwatch.Frequency;

        public static bool MultiProcessor { get; private set; }

        public static int ProcessorCount { get; private set; }

        public static string BaseDirectory
        {
            get
            {
                if (m_BaseDirectory == null)
                {
                    try
                    {
                        m_BaseDirectory = Assembly.Location;

                        if (m_BaseDirectory.Length > 0)
                        {
                            m_BaseDirectory = Path.GetDirectoryName(m_BaseDirectory);
                        }
                    }
                    catch
                    {
                        m_BaseDirectory = "";
                    }
                }

                return m_BaseDirectory;
            }
        }

        public static CancellationTokenSource ClosingTokenSource { get; } = new CancellationTokenSource();

        public static bool Closing => ClosingTokenSource.IsCancellationRequested;

        public static float CyclesPerSecond => m_CyclesPerSecond[(m_CycleIndex - 1) % m_CyclesPerSecond.Length];

        public static float AverageCPS => m_CyclesPerSecond.Take(m_CycleIndex).Average();

        public static string Arguments
        {
            get
            {
                var sb = new StringBuilder();

                if (m_Profiling)
                {
                    Utility.Separate(sb, "-profile", " ");
                }

                return sb.ToString();
            }
        }

        public static int GlobalUpdateRange { get; set; } = 18;

        public static int GlobalMaxUpdateRange { get; set; } = 24;

        public static int ScriptItems => m_ItemCount;
        public static int ScriptMobiles => m_MobileCount;

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

        public static string FindDataFile(string path, bool throwNotFound = true, bool warnNotFound = false)
        {
            string fullPath = null;

            foreach (var p in ServerConfiguration.DataDirectories)
            {
                fullPath = Path.Combine(p, path);

                if (File.Exists(fullPath))
                {
                    break;
                }

                fullPath = null;
            }

            if (fullPath == null && (throwNotFound || warnNotFound))
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine($"Data: {path} was not found");
                Console.WriteLine("Make sure modernuo.json is properly configured");
                Utility.PopColor();
                if (throwNotFound)
                {
                    throw new FileNotFoundException($"Data: {path} was not found");
                }
            }

            return fullPath;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.IsTerminating ? "Error:" : "Warning:");
            Console.WriteLine(e.ExceptionObject);

            if (e.IsTerminating)
            {
                m_Crashed = true;

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
                _ => "CTRL+C"
            };

            Console.WriteLine("Core: Detected {0} pressed.", keypress);
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

            Console.Write("Core: Shutting down...");

            World.WaitForWriteCompletion();

            if (!m_Crashed)
            {
                EventSink.InvokeShutdown();
            }

            Timer.TimerThread.Set();

            Console.WriteLine("done");
        }

        public static void Set()
        {
            // m_Signal.Set();
        }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            foreach (var a in args)
            {
                if (Insensitive.Equals(a, "-profile"))
                {
                    Profiling = true;
                }
            }

            Thread = Thread.CurrentThread;
            Process = Process.GetCurrentProcess();
            Assembly = Assembly.GetEntryAssembly();

            if (Assembly == null)
            {
                throw new Exception("Core: Assembly entry is missing.");
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
            Console.WriteLine(@"Copyright 2019-2020 ModernUO Development Team
                This program comes with ABSOLUTELY NO WARRANTY;
                This is free software, and you are welcome to redistribute it under certain conditions.

                You should have received a copy of the GNU General Public License
                along with this program. If not, see <https://www.gnu.org/licenses/>.
            ".TrimMultiline());
            Utility.PopColor();

            Console.WriteLine("Core: Running on {0}", RuntimeInformation.FrameworkDescription);

            var ttObj = new Timer.TimerThread();
            timerThread = new Thread(ttObj.TimerMain)
            {
                Name = "Timer Thread"
            };

            var s = Arguments;

            if (s.Length > 0)
            {
                Console.WriteLine("Core: Running with arguments: {0}", s);
            }

            ProcessorCount = Environment.ProcessorCount;

            if (ProcessorCount > 1)
            {
                MultiProcessor = true;
            }

            if (MultiProcessor)
            {
                Console.WriteLine("Core: Optimizing for {0} processor{1}", ProcessorCount, ProcessorCount == 1 ? "" : "s");
            }

            Console.CancelKeyPress += Console_CancelKeyPressed;

            if (GCSettings.IsServerGC)
            {
                Console.WriteLine("Core: Server garbage collection mode enabled");
            }

            Console.WriteLine(
                "Core: High resolution timing ({0})",
                Stopwatch.IsHighResolution ? "Supported" : "Unsupported"
            );

            ServerConfiguration.Load();

            var assemblyPath = Path.Join(BaseDirectory, assembliesConfiguration);

            // Load UOContent.dll
            var assemblyFiles = JsonConfig.Deserialize<List<string>>(assemblyPath)
                .Select(t => Path.Join(BaseDirectory, "Assemblies", t))
                .ToArray();

            AssemblyHandler.LoadScripts(assemblyFiles);

            VerifySerialization();

            AssemblyHandler.Invoke("Configure");

            RegionLoader.LoadRegions();
            World.Load();

            AssemblyHandler.Invoke("Initialize");

            timerThread.Start();

            foreach (var m in Map.AllMaps)
            {
                m.Tiles.Force();
            }

            TcpServer.Start();
            EventSink.InvokeServerStarted();
            RunEventLoop();
        }

        public static void RunEventLoop()
        {
            try
            {
                long last = TickCount;

                const int sampleInterval = 100;
                const float ticksPerSecond = 1000.0f * sampleInterval;
                float cyclesPerSecond;

                long sample = 0;

                while (!Closing)
                {
                    // m_Signal.WaitOne();

                    Task.WaitAll(
                        Task.Run(Mobile.ProcessDeltaQueue),
                        Task.Run(Item.ProcessDeltaQueue)
                    );

                    Timer.Slice();

                    // Handle networking
                    TcpServer.Slice();
                    NetState.HandleAllReceives();
                    NetState.FlushAll();
                    NetState.ProcessDisposedQueue();

                    // Execute other stuff

                    if (sample++ % sampleInterval != 0)
                    {
                        continue;
                    }

                    var now = TickCount;
                    cyclesPerSecond = ticksPerSecond / (now - last);
                    m_CyclesPerSecond[m_CycleIndex++ % m_CyclesPerSecond.Length] = cyclesPerSecond;
                    last = now;

                    if (cyclesPerSecond > 80)
                    {
                        Thread.Sleep(2);
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
            m_ItemCount = 0;
            m_MobileCount = 0;

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
            var isItem = type.IsSubclassOf(typeof(Item));

            if (!isItem && !type.IsSubclassOf(typeof(Mobile)))
            {
                return;
            }

            if (isItem)
            {
                Interlocked.Increment(ref m_ItemCount);
            }
            else
            {
                Interlocked.Increment(ref m_MobileCount);
            }

            StringBuilder warningSb = null;

            try
            {
                if (type.GetConstructor(m_SerialTypeArray) == null)
                {
                    warningSb = new StringBuilder();
                    warningSb.AppendLine("       - No serialization constructor");
                }

                const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                  BindingFlags.Instance | BindingFlags.DeclaredOnly;
                if (type.GetMethod("Serialize", bindingFlags) == null)
                {
                    warningSb ??= new StringBuilder();
                    warningSb.AppendLine("       - No Serialize() method");
                }

                if (type.GetMethod("Deserialize", bindingFlags) == null)
                {
                    warningSb ??= new StringBuilder();
                    warningSb.AppendLine("       - No Deserialize() method");
                }

                if (warningSb?.Length > 0)
                {
                    Console.WriteLine("Warning: {0}\n{1}", type, warningSb);
                }
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
}
