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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Network;
using System.Runtime;

namespace Server
{
	public delegate void Slice();

	public static class Core
	{
		private static bool m_Crashed;
		private static Thread timerThread;
		private static string m_BaseDirectory;
		private static string m_ExePath;

		private static readonly List<string> m_DataDirectories = new List<string>();

		private static Assembly m_Assembly;
		private static Process m_Process;
		private static Thread m_Thread;
		private static bool m_Service;
		private static bool m_Debug;
		private static bool m_Cache = true;
		private static bool m_HaltOnWarning;
		private static bool m_VBdotNET;
		private static MultiTextWriter m_MultiConOut;

		private static bool m_Profiling;
		private static DateTime m_ProfileStart;
		private static TimeSpan m_ProfileTime;

		private static MessagePump m_MessagePump;

		public static MessagePump MessagePump
		{
			get { return m_MessagePump; }
			set { m_MessagePump = value; }
		}

		public static Slice Slice;

		public static bool Profiling
		{
			get { return m_Profiling; }
			set
			{
				if( m_Profiling == value )
					return;

				m_Profiling = value;

				if( m_ProfileStart > DateTime.MinValue )
					m_ProfileTime += DateTime.UtcNow - m_ProfileStart;

				m_ProfileStart = (m_Profiling ? DateTime.UtcNow : DateTime.MinValue);
			}
		}

		public static TimeSpan ProfileTime
		{
			get
			{
				if( m_ProfileStart > DateTime.MinValue )
					return m_ProfileTime + (DateTime.UtcNow - m_ProfileStart);

				return m_ProfileTime;
			}
		}

		public static bool Service { get { return m_Service; } }
		public static bool Debug { get { return m_Debug; } }
		internal static bool HaltOnWarning { get { return m_HaltOnWarning; } }
		internal static bool VBdotNet { get { return m_VBdotNET; } }
		public static List<string> DataDirectories { get { return m_DataDirectories; } }
		public static Assembly Assembly { get { return m_Assembly; } set { m_Assembly = value; } }
		public static Version Version { get { return m_Assembly.GetName().Version; } }
		public static Process Process { get { return m_Process; } }
		public static Thread Thread { get { return m_Thread; } }
		public static MultiTextWriter MultiConsoleOut { get { return m_MultiConOut; } }

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

		public static bool UsingHighResolutionTiming { get { return _UseHRT && _HighRes && !Unix; } }

		public static long TickCount { get { return (long)Ticks; } }

		public static double Ticks
		{
			get
			{
				if (_UseHRT && _HighRes && !Unix)
				{
					return Stopwatch.GetTimestamp() * _HighFrequency;
				}

				return DateTime.UtcNow.Ticks * _LowFrequency;
			}
		}

		public static readonly bool Is64Bit = Environment.Is64BitProcess;

		private static bool m_MultiProcessor;
		private static int m_ProcessorCount;

		public static bool MultiProcessor { get { return m_MultiProcessor; } }
		public static int ProcessorCount { get { return m_ProcessorCount; } }

		private static bool m_Unix;
		
		public static bool Unix { get { return m_Unix; } }

		public static string FindDataFile( string path )
		{
			if( m_DataDirectories.Count == 0 )
				throw new InvalidOperationException( "Attempted to FindDataFile before DataDirectories list has been filled." );

			string fullPath = null;

			foreach (string p in m_DataDirectories)
			{
				fullPath = Path.Combine( p, path );

				if( File.Exists( fullPath ) )
					break;

				fullPath = null;
			}

			return fullPath;
		}

		public static string FindDataFile( string format, params object[] args )
		{
			return FindDataFile( String.Format( format, args ) );
		}

		#region Expansions

		private static Expansion m_Expansion;
		public static Expansion Expansion
		{
			get { return m_Expansion; }
			set { m_Expansion = value; }
		}

		public static bool T2A
		{
			get { return m_Expansion >= Expansion.T2A; }
		}

		public static bool UOR
		{
			get { return m_Expansion >= Expansion.UOR; }
		}

		public static bool UOTD
		{
			get { return m_Expansion >= Expansion.UOTD; }
		}

		public static bool LBR
		{
			get { return m_Expansion >= Expansion.LBR; }
		}

		public static bool AOS
		{
			get { return m_Expansion >= Expansion.AOS; }
		}

		public static bool SE
		{
			get { return m_Expansion >= Expansion.SE; }
		}

		public static bool ML
		{
			get { return m_Expansion >= Expansion.ML; }
		}

		public static bool SA
		{
			get { return m_Expansion >= Expansion.SA; }
		}

		public static bool HS
		{
			get { return m_Expansion >= Expansion.HS; }
		}

		public static bool TOL
		{
			get { return m_Expansion >= Expansion.TOL; }
		}

		#endregion

		public static string ExePath
		{
			get { return m_ExePath ?? (m_ExePath = Assembly.Location); }
		}

		public static string BaseDirectory
		{
			get
			{
				if( m_BaseDirectory == null )
				{
					try
					{
						m_BaseDirectory = ExePath;

						if( m_BaseDirectory.Length > 0 )
							m_BaseDirectory = Path.GetDirectoryName( m_BaseDirectory );
					}
					catch
					{
						m_BaseDirectory = "";
					}
				}

				return m_BaseDirectory;
			}
		}

		private static void CurrentDomain_UnhandledException( object sender, UnhandledExceptionEventArgs e )
		{
			Console.WriteLine( e.IsTerminating ? "Error:" : "Warning:" );
			Console.WriteLine( e.ExceptionObject );

			if( e.IsTerminating )
			{
				m_Crashed = true;

				bool close = false;

				try
				{
					CrashedEventArgs args = new CrashedEventArgs( e.ExceptionObject as Exception );

					EventSink.InvokeCrashed( args );

					close = args.Close;
				}
				catch
				{
				}

				if( !close && !m_Service )
				{
					try
					{
						foreach (Listener l in m_MessagePump.Listeners)
						{
							l.Dispose();
						}
					}
					catch
					{
					}

					Console.WriteLine( "This exception is fatal, press return to exit" );
					Console.ReadLine();
				}

				Kill();
			}
		}

		internal enum ConsoleEventType
		{
			CTRL_C_EVENT,
			CTRL_BREAK_EVENT,
			CTRL_CLOSE_EVENT,
			CTRL_LOGOFF_EVENT=5,
			CTRL_SHUTDOWN_EVENT
		}

		internal delegate bool ConsoleEventHandler( ConsoleEventType type );
		internal static ConsoleEventHandler m_ConsoleEventHandler;

		internal class UnsafeNativeMethods {
			[DllImport("Kernel32")]
			internal static extern bool SetConsoleCtrlHandler(ConsoleEventHandler callback, bool add);
		}

		private static bool OnConsoleEvent( ConsoleEventType type )
		{
			if( World.Saving || ( m_Service && type == ConsoleEventType.CTRL_LOGOFF_EVENT ) )
				return true;
			
			Kill();	//Kill -> HandleClosed will handle waiting for the completion of flushing to disk

			return true;
		}

		private static void CurrentDomain_ProcessExit( object sender, EventArgs e )
		{
			HandleClosed();
		}

		private static bool m_Closing;
		public static bool Closing { get { return m_Closing; } }

		private static int m_CycleIndex = 1;
		private static readonly float[] m_CyclesPerSecond = new float[100];

		public static float CyclesPerSecond { get { return m_CyclesPerSecond[(m_CycleIndex - 1) % m_CyclesPerSecond.Length]; } }

		public static float AverageCPS { get { return m_CyclesPerSecond.Take(m_CycleIndex).Average(); } }

		public static void Kill()
		{
			Kill( false );
		}

		public static void Kill( bool restart )
		{
			HandleClosed();

			if ( restart )
				Process.Start( ExePath, Arguments );

			m_Process.Kill();
		}

		private static void HandleClosed()
		{
			if( m_Closing )
				return;

			m_Closing = true;

			Console.Write( "Exiting..." );

			World.WaitForWriteCompletion();

			if( !m_Crashed )
				EventSink.InvokeShutdown( new ShutdownEventArgs() );

			Timer.TimerThread.Set();

			Console.WriteLine( "done" );
		}

		private static readonly AutoResetEvent m_Signal = new AutoResetEvent( true );

		public static void Set() { m_Signal.Set(); }

		public static void Main( string[] args )
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

			foreach (string a in args)
			{
				if ( Insensitive.Equals( a, "-debug" ) )
					m_Debug = true;
				else if ( Insensitive.Equals( a, "-service" ) )
					m_Service = true;
				else if ( Insensitive.Equals( a, "-profile" ) )
					Profiling = true;
				else if ( Insensitive.Equals( a, "-nocache" ) )
					m_Cache = false;
				else if ( Insensitive.Equals( a, "-haltonwarning" ) )
					m_HaltOnWarning = true;
				else if ( Insensitive.Equals( a, "-vb" ) )
					m_VBdotNET = true;
				else if ( Insensitive.Equals( a, "-usehrt" ) )
					_UseHRT = true;
			}

			try
			{
				if( m_Service )
				{
					if( !Directory.Exists( "Logs" ) )
						Directory.CreateDirectory( "Logs" );

					Console.SetOut( m_MultiConOut = new MultiTextWriter( new FileLogger( "Logs/Console.log" ) ) );
				}
				else
				{
					Console.SetOut( m_MultiConOut = new MultiTextWriter( Console.Out ) );
				}
			}
			catch
			{
			}

			m_Thread = Thread.CurrentThread;
			m_Process = Process.GetCurrentProcess();
			m_Assembly = Assembly.GetEntryAssembly();

			if( m_Thread != null )
				m_Thread.Name = "Core Thread";

			if( BaseDirectory.Length > 0 )
				Directory.SetCurrentDirectory( BaseDirectory );

			Timer.TimerThread ttObj = new Timer.TimerThread();
			timerThread = new Thread(ttObj.TimerMain)
			{
				Name = "Timer Thread"
			};

			Version ver = m_Assembly.GetName().Version;

			// Added to help future code support on forums, as a 'check' people can ask for to it see if they recompiled core or not
			Console.WriteLine("RunUO - [https://github.com/runuo/] Version {0}.{1}.{2}.{3}", ver.Major, ver.Minor, ver.Build, ver.Revision);
			Console.WriteLine("Core: Running on .NET Framework Version {0}.{1}.{2}", Environment.Version.Major, Environment.Version.Minor, Environment.Version.Build);

			string s = Arguments;

			if( s.Length > 0 )
				Console.WriteLine( "Core: Running with arguments: {0}", s );

			m_ProcessorCount = Environment.ProcessorCount;

			if( m_ProcessorCount > 1 )
				m_MultiProcessor = true;

			if( m_MultiProcessor || Is64Bit )
				Console.WriteLine( "Core: Optimizing for {0} {2}processor{1}", m_ProcessorCount, m_ProcessorCount == 1 ? "" : "s", Is64Bit ? "64-bit " : "" );

			int platform = (int)Environment.OSVersion.Platform;
			if( platform == 4 || platform == 128 ) { // MS 4, MONO 128
				m_Unix = true;
				Console.WriteLine( "Core: Unix environment detected" );
			}
			else {
				m_ConsoleEventHandler = OnConsoleEvent;
				UnsafeNativeMethods.SetConsoleCtrlHandler( m_ConsoleEventHandler, true );
			}

			if ( GCSettings.IsServerGC )
				Console.WriteLine("Core: Server garbage collection mode enabled");

			if (_UseHRT)
				Console.WriteLine("Core: Requested high resolution timing ({0})", UsingHighResolutionTiming ? "Supported" : "Unsupported");

			Console.WriteLine("RandomImpl: {0} ({1})", RandomImpl.Type.Name, RandomImpl.IsHardwareRNG ? "Hardware" : "Software");

			while( !ScriptCompiler.Compile( m_Debug, m_Cache ) )
			{
				Console.WriteLine( "Scripts: One or more scripts failed to compile or no script files were found." );
				
				if( m_Service )
					return;

				Console.WriteLine( " - Press return to exit, or R to try again." );
				
				if( Console.ReadKey( true ).Key != ConsoleKey.R )
					return;
			}

			ScriptCompiler.Invoke( "Configure" );
			
			Region.Load();
			World.Load();

			ScriptCompiler.Invoke( "Initialize" );

			MessagePump messagePump = m_MessagePump = new MessagePump();

			timerThread.Start();

			foreach (Map m in Map.AllMaps)
				m.Tiles.Force();

			NetState.Initialize();

			EventSink.InvokeServerStarted();

			try
			{
				long now, last = TickCount;

				const int sampleInterval = 100;
				const float ticksPerSecond = 1000.0f * sampleInterval;

				long sample = 0;

				while( !m_Closing )
				{
					m_Signal.WaitOne();

					Mobile.ProcessDeltaQueue();
					Item.ProcessDeltaQueue();

					Timer.Slice();
					messagePump.Slice();

					NetState.FlushAll();
					NetState.ProcessDisposedQueue();

					if( Slice != null )
						Slice();

					if (sample++ % sampleInterval != 0)
					{
						continue;
					}

					now = TickCount;
					m_CyclesPerSecond[m_CycleIndex++ % m_CyclesPerSecond.Length] = ticksPerSecond / (now - last);
					last = now;
				}
			}
			catch( Exception e )
			{
				CurrentDomain_UnhandledException( null, new UnhandledExceptionEventArgs( e, true ) );
			}
		}

		public static string Arguments
		{
			get
			{
				StringBuilder sb = new StringBuilder();

				if( m_Debug )
					Utility.Separate( sb, "-debug", " " );

				if( m_Service )
					Utility.Separate( sb, "-service", " " );

				if( m_Profiling )
					Utility.Separate( sb, "-profile", " " );

				if( !m_Cache )
					Utility.Separate( sb, "-nocache", " " );

				if( m_HaltOnWarning )
					Utility.Separate( sb, "-haltonwarning", " " );

				if ( m_VBdotNET )
					Utility.Separate( sb, "-vb", " " );

				if ( _UseHRT )
					Utility.Separate( sb, "-usehrt", " " );

				return sb.ToString();
			}
		}

		private static int m_GlobalUpdateRange = 18;

		public static int GlobalUpdateRange
		{
			get { return m_GlobalUpdateRange; }
			set { m_GlobalUpdateRange = value; }
		}

		private static int m_GlobalMaxUpdateRange = 24;

		public static int GlobalMaxUpdateRange
		{
			get { return m_GlobalMaxUpdateRange; }
			set { m_GlobalMaxUpdateRange = value; }
		}

		private static int m_ItemCount, m_MobileCount;

		public static int ScriptItems { get { return m_ItemCount; } }
		public static int ScriptMobiles { get { return m_MobileCount; } }

		public static void VerifySerialization()
		{
			m_ItemCount = 0;
			m_MobileCount = 0;

			var ca = Assembly.GetCallingAssembly();

			VerifySerialization(ca);

			foreach (var a in ScriptCompiler.Assemblies.Where(a => a != ca))
			{
				VerifySerialization(a);
			}
		}

		private static readonly Type[] m_SerialTypeArray = { typeof(Serial) };
	
		private static void VerifyType(Type t)
		{
			var isItem = t.IsSubclassOf(typeof(Item));

			if (!isItem && !t.IsSubclassOf(typeof(Mobile)))
			{
				return;
			}

			if (isItem)
			{
				//++_ItemCount;
				Interlocked.Increment(ref m_ItemCount);
			}
			else
			{
				//++_MobileCount;
				Interlocked.Increment(ref m_MobileCount);
			}

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
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null)
				{
					if (warningSb == null)
					{
						warningSb = new StringBuilder();
					}

					warningSb.AppendLine("       - No Serialize() method");
				}

				if (
					t.GetMethod(
						"Deserialize",
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly) == null)
				{
					if (warningSb == null)
					{
						warningSb = new StringBuilder();
					}

					warningSb.AppendLine("       - No Deserialize() method");
				}

				if (warningSb != null && warningSb.Length > 0)
				{
					Console.WriteLine("Warning: {0}\n{1}", t, warningSb);
				}
			}
			catch
			{
				Console.WriteLine("Warning: Exception in serialization verification of type {0}", t);
			}
		}

		private static void VerifySerialization(Assembly a)
		{
			if (a != null)
			{
				Parallel.ForEach(a.GetTypes(), VerifyType);
			}
		}
	}

	public class FileLogger : TextWriter
	{
		public const string DateFormat = "[MMMM dd hh:mm:ss.f tt]: ";

		private bool _NewLine;

		public string FileName { get; private set; }

		public FileLogger(string file)
			: this(file, false)
		{ }

		public FileLogger(string file, bool append)
		{
			FileName = file;

			using (
				var writer =
					new StreamWriter(
						new FileStream(FileName, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read)))
			{
				writer.WriteLine(">>>Logging started on {0}.", DateTime.UtcNow.ToString("f"));
				//f = Tuesday, April 10, 2001 3:51 PM 
			}

			_NewLine = true;
		}

		public override void Write(char ch)
		{
			using (var writer = new StreamWriter(new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
			{
				if (_NewLine)
				{
					writer.Write(DateTime.UtcNow.ToString(DateFormat));
					_NewLine = false;
				}

				writer.Write(ch);
			}
		}

		public override void Write(string str)
		{
			using (var writer = new StreamWriter(new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
			{
				if (_NewLine)
				{
					writer.Write(DateTime.UtcNow.ToString(DateFormat));
					_NewLine = false;
				}

				writer.Write(str);
			}
		}

		public override void WriteLine(string line)
		{
			using (var writer = new StreamWriter(new FileStream(FileName, FileMode.Append, FileAccess.Write, FileShare.Read)))
			{
				if (_NewLine)
				{
					writer.Write(DateTime.UtcNow.ToString(DateFormat));
				}

				writer.WriteLine(line);
				_NewLine = true;
			}
		}

		public override Encoding Encoding { get { return Encoding.Default; } }
	}

	public class MultiTextWriter : TextWriter
	{
		private readonly List<TextWriter> _Streams;

		public MultiTextWriter(params TextWriter[] streams)
		{
			_Streams = new List<TextWriter>(streams);

			if (_Streams.Count < 0)
			{
				throw new ArgumentException("You must specify at least one stream.");
			}
		}

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
			foreach (var t in _Streams)
			{
				t.Write(ch);
			}
		}

		public override void WriteLine(string line)
		{
			foreach (var t in _Streams)
			{
				t.WriteLine(line);
			}
		}

		public override void WriteLine(string line, params object[] args)
		{
			WriteLine(String.Format(line, args));
		}

		public override Encoding Encoding { get { return Encoding.Default; } }
	}
}