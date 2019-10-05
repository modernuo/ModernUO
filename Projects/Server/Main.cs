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
using Server.Network;

namespace Server
{
  public delegate void Slice();

  public static class Core
  {
    private static bool m_Crashed;
    private static Thread timerThread;
    private static string m_BaseDirectory;
    private static string m_ExePath;

    private static bool m_Cache = true;

    private static bool m_Profiling;
    private static DateTime m_ProfileStart;
    private static TimeSpan m_ProfileTime;

    public static Slice Slice;

    /*
     * DateTime.Now and DateTime.UtcNow are based on actual system clock time.
     * The resolution is acceptable but large clock jumps are possible and cause issues.
     * GetTickCount and GetTickCount64 have poor resolution.
     * GetTickCount64 is unavailable on Windows XP and Windows Server 2003.
     * Stopwatch.GetTimestamp() (QueryPerformanceCounter) is high resolution, but
     * somewhat expensive to call because of its defference to DateTime.Now,
     * which is why Stopwatch has been used to verify HRT before calling GetTimestamp(),
     * enabling the usage of DateTime.UtcNow instead.
     */

    private static readonly bool _HighRes = Stopwatch.IsHighResolution;

    private static readonly double _HighFrequency = 1000.0 / Stopwatch.Frequency;
    private static readonly double _LowFrequency = 1000.0 / TimeSpan.TicksPerSecond;

    private static bool _UseHRT;

    public static readonly bool Is64Bit = Environment.Is64BitProcess;
    internal static ConsoleEventHandler m_ConsoleEventHandler;

    private static int m_CycleIndex = 1;
    private static readonly float[] m_CyclesPerSecond = new float[100];

    private static readonly AutoResetEvent m_Signal = new AutoResetEvent(true);

    private static int m_ItemCount, m_MobileCount;

    private static readonly Type[] m_SerialTypeArray = { typeof(Serial) };

    public static MessagePump MessagePump{ get; set; }

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

    public static bool Service{ get; private set; }

    public static bool Debug { get; private set; }

    internal static bool HaltOnWarning{ get; private set; }

    public static Assembly Assembly{ get; set; }

    public static Version Version => Assembly.GetName().Version;
    public static Process Process{ get; private set; }

    public static Thread Thread{ get; private set; }

    public static MultiTextWriter MultiConsoleOut{ get; private set; }

    public static bool UsingHighResolutionTiming => _UseHRT && _HighRes && !Unix;

    public static long TickCount => (long)Ticks;

    public static double Ticks
    {
      get
      {
        if (_UseHRT && _HighRes && !Unix) return Stopwatch.GetTimestamp() * _HighFrequency;

        return DateTime.UtcNow.Ticks * _LowFrequency;
      }
    }

    public static bool MultiProcessor{ get; private set; }

    public static int ProcessorCount{ get; private set; }

    public static bool Unix{ get; private set; }

    public static string ExePath => m_ExePath ?? (m_ExePath = Assembly.Location);

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

    public static bool Closing{ get; private set; }

    public static float CyclesPerSecond => m_CyclesPerSecond[(m_CycleIndex - 1) % m_CyclesPerSecond.Length];

    public static float AverageCPS => m_CyclesPerSecond.Take(m_CycleIndex).Average();

    public static string Arguments
    {
      get
      {
        StringBuilder sb = new StringBuilder();

        if (Debug)
          Utility.Separate(sb, "-debug", " ");

        if (Service)
          Utility.Separate(sb, "-service", " ");

        if (m_Profiling)
          Utility.Separate(sb, "-profile", " ");

        if (!m_Cache)
          Utility.Separate(sb, "-nocache", " ");

        if (HaltOnWarning)
          Utility.Separate(sb, "-haltonwarning", " ");

        if (_UseHRT)
          Utility.Separate(sb, "-usehrt", " ");

        return sb.ToString();
      }
    }

    public static int GlobalUpdateRange{ get; set; } = 18;

    public static int GlobalMaxUpdateRange{ get; set; } = 24;

    public static int ScriptItems => m_ItemCount;
    public static int ScriptMobiles => m_MobileCount;

    public static string FindDataFile(string path)
    {
      Configuration config = Configuration.Instance;
      if (config.DataDirectories.Count == 0)
        throw new InvalidOperationException(
          "Attempted to FindDataFile before DataDirectories list has been filled.");

      string fullPath = null;

      foreach (string p in config.DataDirectories)
      {
        fullPath = Path.Combine(p, path);

        if (File.Exists(fullPath))
          break;

        fullPath = null;
      }

      return fullPath;
    }

    public static string FindDataFile(string format, params object[] args) => FindDataFile(string.Format(format, args));

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      Console.WriteLine(e.IsTerminating ? "Error:" : "Warning:");
      Console.WriteLine(e.ExceptionObject);

      if (e.IsTerminating)
      {
        m_Crashed = true;

        bool close = false;

        try
        {
          CrashedEventArgs args = new CrashedEventArgs(e.ExceptionObject as Exception);

          EventSink.InvokeCrashed(args);

          close = args.Close;
        }
        catch
        {
          // ignored
        }

        if (!close && !Service)
        {
          try
          {
            foreach (Listener l in MessagePump.Listeners)
              l.Dispose();
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
      if (World.Saving || Service && type == ConsoleEventType.CTRL_LOGOFF_EVENT)
        return true;

      Kill(); //Kill -> HandleClosed will handle waiting for the completion of flushing to disk

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

      Console.WriteLine("Exiting...");

      World.WaitForWriteCompletion();

      if (!m_Crashed)
        EventSink.InvokeShutdown(new ShutdownEventArgs());

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

      foreach (string a in args)
        if (Insensitive.Equals(a, "-debug"))
          Debug = true;
        else if (Insensitive.Equals(a, "-service"))
          Service = true;
        else if (Insensitive.Equals(a, "-profile"))
          Profiling = true;
        else if (Insensitive.Equals(a, "-nocache"))
          m_Cache = false;
        else if (Insensitive.Equals(a, "-haltonwarning"))
          HaltOnWarning = true;
        else if (Insensitive.Equals(a, "-usehrt"))
          _UseHRT = true;

      try
      {
        if (Service)
        {
          if (!Directory.Exists("Logs"))
            Directory.CreateDirectory("Logs");

          Console.SetOut(MultiConsoleOut = new MultiTextWriter(new FileLogger("Logs/Console.log")));
        }
        else
        {
          Console.SetOut(MultiConsoleOut = new MultiTextWriter(Console.Out));
        }
      }
      catch
      {
        // ignored
      }

      Thread = Thread.CurrentThread;
      Process = Process.GetCurrentProcess();
      Assembly = Assembly.GetEntryAssembly();

      if (Thread != null)
        Thread.Name = "Core Thread";

      if (BaseDirectory.Length > 0)
        Directory.SetCurrentDirectory(BaseDirectory);


      Version ver = Assembly.GetName().Version;

      Console.ForegroundColor = ConsoleColor.Green;
      // Added to help future code support on forums, as a 'check' people can ask for to it see if they recompiled core or not
      Console.WriteLine("ModernUO - [https://github.com/kamronbatman/ModernUO] Version {0}.{1}.{2}.{3}", ver.Major, ver.Minor, ver.Build,
        ver.Revision);
      Console.WriteLine("Core: Running on {0}", RuntimeInformation.FrameworkDescription);
      Console.ResetColor();
      Console.WriteLine();

      Configuration config = Configuration.Instance;
      foreach (string dir in config.DataDirectories.Where(dir => !Directory.Exists(dir)))
      {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("Core: Config directory {0} does not exist.", dir);
        Console.ResetColor();
      }

      Timer.TimerThread ttObj = new Timer.TimerThread();
      timerThread = new Thread(ttObj.TimerMain)
      {
        Name = "Timer Thread"
      };

      string s = Arguments;

      if (s.Length > 0)
        Console.WriteLine("Core: Running with arguments: {0}", s);

      ProcessorCount = Environment.ProcessorCount;

      if (ProcessorCount > 1)
        MultiProcessor = true;

      if (MultiProcessor || Is64Bit)
        Console.WriteLine("Core: Optimizing for {0} {2}processor{1}", ProcessorCount, ProcessorCount == 1 ? "" : "s",
          Is64Bit ? "64-bit " : "");

      int platform = (int)Environment.OSVersion.Platform;
      if (platform == 4 || platform == 128)
      {
        // MS 4, MONO 128
        Unix = true;
        Console.WriteLine("Core: Unix environment detected");
      }
      else
      {
        m_ConsoleEventHandler = OnConsoleEvent;
        UnsafeNativeMethods.SetConsoleCtrlHandler(m_ConsoleEventHandler, true);
      }

      if (GCSettings.IsServerGC)
        Console.WriteLine("Core: Server garbage collection mode enabled");

      if (_UseHRT)
        Console.WriteLine("Core: Requested high resolution timing ({0})",
          UsingHighResolutionTiming ? "Supported" : "Unsupported");

      Console.WriteLine("RandomImpl: {0} ({1})", RandomImpl.Type.Name,
        RandomImpl.IsHardwareRNG ? "Hardware" : "Software");

      // Load Assembly Scripts.CS.dll
      AssemblyHandler.LoadScripts();

      AssemblyHandler.Invoke("Configure");

      Region.Load();
      World.Load();

      AssemblyHandler.Invoke("Initialize");

      // Start accepting new connections
      MessagePump = new MessagePump();

      AssemblyHandler.Invoke("RegisterListeners");

      timerThread.Start();

      foreach (Map m in Map.AllMaps)
        m.Tiles.Force();

      NetState.Initialize();

      EventSink.InvokeServerStarted();

      try
      {
        long last = TickCount;

        const int sampleInterval = 100;
        const float ticksPerSecond = 1000.0f * sampleInterval;

        long sample = 0;

        while (!Closing)
        {
          m_Signal.WaitOne();

          Task.WaitAll(
            Task.Run(() => Mobile.ProcessDeltaQueue()),
            Task.Run(() => Item.ProcessDeltaQueue())
          );

          Timer.Slice();
          MessagePump.DoWork();

          NetState.ProcessDisposedQueue();

          Slice?.Invoke();

          if (sample++ % sampleInterval != 0)
            continue;

          long now = TickCount;
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

      Assembly ca = Assembly.GetCallingAssembly();

      VerifySerialization(ca);

      foreach (Assembly a in AssemblyHandler.Assemblies.Where(a => a != ca)) VerifySerialization(a);
    }

    private static void VerifyType(Type t)
    {
      bool isItem = t.IsSubclassOf(typeof(Item));

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
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) ==
          null)
        {
          if (warningSb == null) warningSb = new StringBuilder();

          warningSb.AppendLine("       - No Serialize() method");
        }

        if (
          t.GetMethod(
            "Deserialize",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) ==
          null)
        {
          if (warningSb == null) warningSb = new StringBuilder();

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

#region Expansions

    public static Expansion Expansion{ get; set; }

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

#endregion
  }

  public class FileLogger : TextWriter
  {
    public const string DateFormat = "[MMMM dd hh:mm:ss.f tt]: ";

    private bool _NewLine;

    public FileLogger(string file, bool append = false)
    {
      FileName = file;

      using (
        StreamWriter writer =
          new StreamWriter(
            new FileStream(FileName, append ? FileMode.Append : FileMode.Create, FileAccess.Write,
              FileShare.Read)))
      {
        writer.WriteLine(">>>Logging started on {0}.", DateTime.UtcNow.ToString("f"));
        //f = Tuesday, April 10, 2001 3:51 PM
      }

      _NewLine = true;
    }

    public string FileName{ get; }

    public override Encoding Encoding => Encoding.Default;

    public override void Write(char ch)
    {
      using StreamWriter writer =
        new StreamWriter(new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read));
      if (_NewLine)
      {
        writer.Write(DateTime.UtcNow.ToString(DateFormat));
        _NewLine = false;
      }

      writer.Write(ch);
    }

    public override void Write(string str)
    {
      using StreamWriter writer =
        new StreamWriter(new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read));
      if (_NewLine)
      {
        writer.Write(DateTime.UtcNow.ToString(DateFormat));
        _NewLine = false;
      }

      writer.Write(str);
    }

    public override void WriteLine(string line)
    {
      using StreamWriter writer =
        new StreamWriter(new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read));
      if (_NewLine) writer.Write(DateTime.UtcNow.ToString(DateFormat));

      writer.WriteLine(line);
      _NewLine = true;
    }
  }

  public class MultiTextWriter : TextWriter
  {
    private readonly List<TextWriter> _Streams;

    public MultiTextWriter(params TextWriter[] streams)
    {
      _Streams = new List<TextWriter>(streams);

      if (_Streams.Count < 0) throw new ArgumentException("You must specify at least one stream.");
    }

    public override Encoding Encoding => Encoding.Default;

    public void Add(TextWriter tw)
    {
      _Streams.Add(tw);
    }

    public void Remove(TextWriter tw)
    {
      _Streams.Remove(tw);
    }

    public override void Write(char ch)
    {
      foreach (TextWriter t in _Streams) t.Write(ch);
    }

    public override void WriteLine(string line)
    {
      foreach (TextWriter t in _Streams) t.WriteLine(line);
    }

    public override void WriteLine(string line, params object[] args)
    {
      WriteLine(string.Format(line, args));
    }
  }
}
