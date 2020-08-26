/***************************************************************************
 *                                  Main.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.Json;
using Server.Network;

namespace Server
{
    public static class Core
    {
        private static bool m_Crashed;
        private static Thread timerThread;
        private static string m_BaseDirectory;
        private static string m_ExePath;

        private static bool m_Profiling;
        private static DateTime m_ProfileStart;
        private static TimeSpan m_ProfileTime;
        private static bool? m_IsRunningFromXUnit;

        /*
         * DateTime.Now and DateTime.UtcNow are based on actual system clock time.
         * The resolution is acceptable but large clock jumps are possible and cause issues.
         * GetTickCount and GetTickCount64 have poor resolution.
         * Stopwatch.GetTimestamp() (QueryPerformanceCounter) is high resolution, but
         * somewhat expensive to call because of its difference to DateTime.Now,
         * which is why Stopwatch has been used to verify HRT before calling GetTimestamp(),
         * enabling the usage of DateTime.UtcNow instead.
         */

        private static readonly double m_HighFrequency = 1000.0 / Stopwatch.Frequency;
        private static readonly double m_LowFrequency = 1000.0 / TimeSpan.TicksPerSecond;

        internal static ConsoleEventHandler m_ConsoleEventHandler;

        private static int m_CycleIndex = 1;
        private static readonly float[] m_CyclesPerSecond = new float[100];

        private static readonly AutoResetEvent m_Signal = new AutoResetEvent(true);

        private static int m_ItemCount, m_MobileCount;

        private static readonly Type[] m_SerialTypeArray = { typeof(Serial) };

        public static bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsFreeBSD = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        public static bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || IsFreeBSD;
        public static bool Unix = IsDarwin || IsFreeBSD || IsLinux;

        private static readonly string assembliesConfiguration = "Configuration/assemblies.json";

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
                    return;

                m_Profiling = value;

                if (m_ProfileStart > DateTime.MinValue)
                    m_ProfileTime += DateTime.UtcNow - m_ProfileStart;

                m_ProfileStart = m_Profiling ? DateTime.UtcNow : DateTime.MinValue;
            }
        }

        public static TimeSpan ProfileTime
        {
            get
            {
                if (m_ProfileStart > DateTime.MinValue)
                    return m_ProfileTime + (DateTime.UtcNow - m_ProfileStart);

                return m_ProfileTime;
            }
        }

        internal static bool HaltOnWarning { get; private set; }

        public static Assembly Assembly { get; set; }

        public static Version Version => Assembly.GetName().Version;
        public static Process Process { get; private set; }

        public static Thread Thread { get; private set; }

        public static long TickCount => (long)Ticks;

        public static double Ticks =>
            Stopwatch.IsHighResolution
                ? Stopwatch.GetTimestamp() * m_HighFrequency
                : DateTime.UtcNow.Ticks * m_LowFrequency;

        public static bool MultiProcessor { get; private set; }

        public static int ProcessorCount { get; private set; }

        public static string ExePath => m_ExePath ??= Assembly.Location;

        public static string BaseDirectory
        {
            get
            {
                if (m_BaseDirectory == null)
                    try
                    {
                        m_BaseDirectory = ExePath;

                        if (m_BaseDirectory.Length > 0)
                            m_BaseDirectory = Path.GetDirectoryName(m_BaseDirectory);
                    }
                    catch
                    {
                        m_BaseDirectory = "";
                    }

                return m_BaseDirectory;
            }
        }

        public static bool Closing { get; private set; }

        public static float CyclesPerSecond => m_CyclesPerSecond[(m_CycleIndex - 1) % m_CyclesPerSecond.Length];

        public static float AverageCPS => m_CyclesPerSecond.Take(m_CycleIndex).Average();

        public static string Arguments
        {
            get
            {
                var sb = new StringBuilder();

                if (m_Profiling)
                    Utility.Separate(sb, "-profile", " ");

                if (HaltOnWarning)
                    Utility.Separate(sb, "-haltonwarning", " ");

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
                    break;

                fullPath = null;
            }

            if (fullPath == null && (throwNotFound || warnNotFound))
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.WriteLine($"Data: {path} was not found");
                Console.WriteLine("Make sure modernuo.json is properly configured");
                Utility.PopColor();
                if (throwNotFound)
                    throw new FileNotFoundException($"Data: {path} was not found");
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
                    try
                    {
                        // Close all listeners
                    }
                    catch
                    {
                        // ignored
                    }

                    Console.WriteLine("This exception is fatal, press return to exit");
                    Console.ReadLine();
                }

                Kill();
            }
        }

        private static bool OnConsoleEvent(ConsoleEventType type)
        {
            if (World.Saving || type == ConsoleEventType.CTRL_LOGOFF_EVENT)
                return true;

            Kill(); // Kill -> HandleClosed will handle waiting for the completion of flushing to disk

            return true;
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            HandleClosed();
        }

        public static void Kill(bool restart = false)
        {
            HandleClosed();

            if (restart)
                Process.Start(ExePath, Arguments);

            Process.Kill();
        }

        private static void HandleClosed()
        {
            if (Closing)
                return;

            Closing = true;

            Console.Write("Exiting...");

            World.WaitForWriteCompletion();

            if (!m_Crashed)
                EventSink.InvokeShutdown();

            Timer.TimerThread.Set();

            Console.WriteLine("done");
        }

        public static void Set()
        {
            m_Signal.Set();
        }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            foreach (var a in args)
                if (Insensitive.Equals(a, "-profile"))
                    Profiling = true;
                else if (Insensitive.Equals(a, "-haltonwarning"))
                    HaltOnWarning = true;

            Thread = Thread.CurrentThread;
            Process = Process.GetCurrentProcess();
            Assembly = Assembly.GetEntryAssembly();

            if (Assembly == null)
                throw new Exception("Core: Assembly entry is missing.");

            if (Thread != null)
                Thread.Name = "Core Thread";

            if (BaseDirectory.Length > 0)
                Directory.SetCurrentDirectory(BaseDirectory);

            var ver = Assembly.GetName().Version ?? new Version();

            Utility.PushColor(ConsoleColor.Green);
            // Added to help future code support on forums, as a 'check' people can ask for to it see if they recompiled core or not
            Console.WriteLine(
                "ModernUO - [https://github.com/modernuo/modernuo] Version {0}.{1}.{2}.{3}",
                ver.Major,
                ver.Minor,
                ver.Build,
                ver.Revision
            );
            Console.WriteLine("Core: Running on {0}\n", RuntimeInformation.FrameworkDescription);
            Utility.PopColor();

            var ttObj = new Timer.TimerThread();
            timerThread = new Thread(ttObj.TimerMain)
            {
                Name = "Timer Thread"
            };

            var s = Arguments;

            if (s.Length > 0)
                Console.WriteLine("Core: Running with arguments: {0}", s);

            ProcessorCount = Environment.ProcessorCount;

            if (ProcessorCount > 1)
                MultiProcessor = true;

            if (MultiProcessor)
                Console.WriteLine("Core: Optimizing for {0} processor{1}", ProcessorCount, ProcessorCount == 1 ? "" : "s");

            if (IsWindows)
            {
                m_ConsoleEventHandler = OnConsoleEvent;
                UnsafeNativeMethods.SetConsoleCtrlHandler(m_ConsoleEventHandler, true);
            }

            if (GCSettings.IsServerGC)
                Console.WriteLine("Core: Server garbage collection mode enabled");

            Console.WriteLine(
                "Core: High resolution timing ({0})",
                Stopwatch.IsHighResolution ? "Supported" : "Unsupported"
            );

            ServerConfiguration.Load();

            // Load UOContent.dll
            var assemblyFiles = JsonConfig.Deserialize<List<string>>(
                    Path.Join(BaseDirectory, assembliesConfiguration)
                )
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
                m.Tiles.Force();

            EventSink.InvokeServerStarted();

            // Start net socket server
            var host = TcpServer.CreateWebHostBuilder().Build();
            var life = host.Services.GetRequiredService<IHostApplicationLifetime>();
            life.ApplicationStopping.Register(() => { Kill(); });

            host.Run();
        }

        public static void RunEventLoop(IMessagePumpService messagePumpService)
        {
            try
            {
                var last = TickCount;

                const int sampleInterval = 100;
                const float ticksPerSecond = 1000.0f * sampleInterval;

                long sample = 0;

                while (!Closing)
                {
                    m_Signal.WaitOne();

                    Task.WaitAll(
                        Task.Run(Mobile.ProcessDeltaQueue),
                        Task.Run(Item.ProcessDeltaQueue)
                    );

                    Timer.Slice();
                    messagePumpService.DoWork();

                    NetState.ProcessDisposedQueue();

                    if (sample++ % sampleInterval != 0)
                        continue;

                    var now = TickCount;
                    m_CyclesPerSecond[m_CycleIndex++ % m_CyclesPerSecond.Length] = ticksPerSecond / (now - last);
                    last = now;
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

            var ca = Assembly.GetCallingAssembly();

            VerifySerialization(ca);

            foreach (var a in AssemblyHandler.Assemblies)
                if (a != ca)
                    VerifySerialization(a);
        }

        private static void VerifyType(Type t)
        {
            var isItem = t.IsSubclassOf(typeof(Item));

            if (!isItem && !t.IsSubclassOf(typeof(Mobile))) return;

            if (isItem)
                Interlocked.Increment(ref m_ItemCount);
            else
                Interlocked.Increment(ref m_MobileCount);

            StringBuilder warningSb = null;

            try
            {
                if (t.GetConstructor(m_SerialTypeArray) == null)
                {
                    warningSb = new StringBuilder();
                    warningSb.AppendLine("       - No serialization constructor");
                }

                if (
                    t.GetMethod(
                        "Serialize",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
                    ) ==
                    null)
                {
                    warningSb ??= new StringBuilder();
                    warningSb.AppendLine("       - No Serialize() method");
                }

                if (t.GetMethod(
                        "Deserialize",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
                    ) ==
                    null)
                {
                    warningSb ??= new StringBuilder();
                    warningSb.AppendLine("       - No Deserialize() method");
                }

                if (warningSb?.Length > 0) Console.WriteLine("Warning: {0}\n{1}", t, warningSb);
            }
            catch
            {
                Console.WriteLine("Warning: Exception in serialization verification of type {0}", t);
            }
        }

        private static void VerifySerialization(Assembly a)
        {
            if (a != null) Parallel.ForEach(a.GetTypes(), VerifyType);
        }

        internal enum ConsoleEventType
        {
            CTRL_C_EVENT,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        internal delegate bool ConsoleEventHandler(ConsoleEventType type);

        internal class UnsafeNativeMethods
        {
            [DllImport("Kernel32")]
            internal static extern bool SetConsoleCtrlHandler(ConsoleEventHandler callback, bool add);
        }
    }
}
