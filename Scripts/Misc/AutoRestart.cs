using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Server;
using Server.Commands;

namespace Server.Misc
{
	public class AutoRestart : Timer
	{
		public static bool Enabled = false; // is the script enabled?

		private static TimeSpan RestartTime = TimeSpan.FromHours( 2.0 ); // time of day at which to restart
		private static TimeSpan RestartDelay = TimeSpan.Zero; // how long the server should remain active before restart (period of 'server wars')

		private static TimeSpan WarningDelay = TimeSpan.FromMinutes( 1.0 ); // at what interval should the shutdown message be displayed?

		private static bool m_Restarting;
		private static DateTime m_RestartTime;

		public static bool Restarting
		{
			get{ return m_Restarting; }
		}

		public static void Initialize()
		{
			CommandSystem.Register( "Restart", AccessLevel.Administrator, new CommandEventHandler( Restart_OnCommand ) );
			new AutoRestart().Start();
		}

		public static void Restart_OnCommand( CommandEventArgs e )
		{
			if ( m_Restarting )
			{
				e.Mobile.SendMessage( "The server is already restarting." );
			}
			else
			{
				e.Mobile.SendMessage( "You have initiated server shutdown." );
				Enabled = true;
				m_RestartTime = DateTime.Now;
			}
		}

		public AutoRestart() : base( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 1.0 ) )
		{
			Priority = TimerPriority.FiveSeconds;

			m_RestartTime = DateTime.Now.Date + RestartTime;

			if ( m_RestartTime < DateTime.Now )
				m_RestartTime += TimeSpan.FromDays( 1.0 );
		}

		private void Warning_Callback()
		{
			World.Broadcast( 0x22, true, "The server is going down shortly." );
		}

		private void Restart_Callback()
		{
			Core.Kill( true );
		}

		protected override void OnTick()
		{
			if ( m_Restarting || !Enabled )
				return;

			if ( DateTime.Now < m_RestartTime )
				return;

			if ( WarningDelay > TimeSpan.Zero )
			{
				Warning_Callback();
				Timer.DelayCall( WarningDelay, WarningDelay, new TimerCallback( Warning_Callback ) );
			}

			AutoSave.Save();

			m_Restarting = true;

			Timer.DelayCall( RestartDelay, new TimerCallback( Restart_Callback ) );
		}
	}
}