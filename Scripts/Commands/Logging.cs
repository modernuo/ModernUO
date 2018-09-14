using System;
using System.IO;
using Server.Accounting;

namespace Server.Commands
{
	public class CommandLogging
	{
		private static StreamWriter m_Output;
		private static bool m_Enabled = true;

		public static bool Enabled{ get => m_Enabled;
			set => m_Enabled = value;
		}

		public static StreamWriter Output => m_Output;

		public static void Initialize()
		{
			EventSink.Command += new CommandEventHandler( EventSink_Command );

			if ( !Directory.Exists( "Logs" ) )
				Directory.CreateDirectory( "Logs" );

			string directory = "Logs/Commands";

			if ( !Directory.Exists( directory ) )
				Directory.CreateDirectory( directory );

			try
			{
				m_Output = new StreamWriter( Path.Combine( directory, $"{DateTime.UtcNow.ToLongDateString()}.log"), true );

				m_Output.AutoFlush = true;

				m_Output.WriteLine( "##############################" );
				m_Output.WriteLine( "Log started on {0}", DateTime.UtcNow );
				m_Output.WriteLine();
			}
			catch
			{
			}
		}

		public static object Format( object o )
		{
			if ( o is Mobile m )
			{
				if ( m.Account == null )
					return $"{m} (no account)";

				return $"{m} ('{m.Account.Username}')";
			}
			if ( o is Item item )
			{
				return $"0x{item.Serial.Value:X} ({item.GetType().Name})";
			}

			return o;
		}

		public static void WriteLine( Mobile from, string format, params object[] args )
		{
			if ( !m_Enabled )
				return;

			WriteLine( from, String.Format( format, args ) );
		}

		public static void WriteLine( Mobile from, string text )
		{
			if ( !m_Enabled )
				return;

			try
			{
				m_Output.WriteLine( "{0}: {1}: {2}", DateTime.UtcNow, from.NetState, text );

				string path = Core.BaseDirectory;

				string name = ( !(from.Account is Account acct) ? from.Name : acct.Username );

				AppendPath( ref path, "Logs" );
				AppendPath( ref path, "Commands" );
				AppendPath( ref path, from.AccessLevel.ToString() );
				path = Path.Combine( path, $"{name}.log");

				using ( StreamWriter sw = new StreamWriter( path, true ) )
					sw.WriteLine( "{0}: {1}: {2}", DateTime.UtcNow, from.NetState, text );
			}
			catch
			{
			}
		}

		private static char[] m_NotSafe = new[]{ '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

		public static void AppendPath( ref string path, string toAppend )
		{
			path = Path.Combine( path, toAppend );

			if ( !Directory.Exists( path ) )
				Directory.CreateDirectory( path );
		}

		public static string Safe( string ip )
		{
			if ( ip == null )
				return "null";

			ip = ip.Trim();

			if ( ip.Length == 0 )
				return "empty";

			bool isSafe = true;

			for ( int i = 0; isSafe && i < m_NotSafe.Length; ++i )
				isSafe = ( ip.IndexOf( m_NotSafe[i] ) == -1 );

			if ( isSafe )
				return ip;

			System.Text.StringBuilder sb = new System.Text.StringBuilder( ip );

			for ( int i = 0; i < m_NotSafe.Length; ++i )
				sb.Replace( m_NotSafe[i], '_' );

			return sb.ToString();
		}

		public static void EventSink_Command( CommandEventArgs e )
		{
			WriteLine( e.Mobile, "{0} {1} used command '{2} {3}'", e.Mobile.AccessLevel, Format( e.Mobile ), e.Command, e.ArgString );
		}

		public static void LogChangeProperty( Mobile from, object o, string name, string value )
		{
			WriteLine( from, "{0} {1} set property '{2}' of {3} to '{4}'", from.AccessLevel, Format( from ), name, Format( o ), value );
		}
	}
}
