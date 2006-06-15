using System;
using System.Collections;
using System.Net.Mail;
using System.IO;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Misc;
using Server.Accounting;
using Server.Engines.Reports;
using Server.Commands;
using System.Collections.Generic;

namespace Server.Engines.Help
{
	public enum PageType
	{
		Bug,
		Stuck,
		Account,
		Question,
		Suggestion,
		Other,
		VerbalHarassment,
		PhysicalHarassment
	}

	public class PageEntry
	{
		// What page types should have a speech log as attachment?
		public static readonly PageType[] SpeechLogAttachment = new PageType[]
			{
				PageType.VerbalHarassment
			};

		private Mobile m_Sender;
		private Mobile m_Handler;
		private DateTime m_Sent;
		private string m_Message;
		private PageType m_Type;
		private Point3D m_PageLocation;
		private Map m_PageMap;
		private List<SpeechLogEntry> m_SpeechLog;

		private PageInfo m_PageInfo;

		public PageInfo PageInfo
		{
			get{ return m_PageInfo; }
		}

		public Mobile Sender
		{
			get
			{
				return m_Sender;
			}
		}

		public Mobile Handler
		{
			get
			{
				return m_Handler;
			}
			set
			{
				PageQueue.OnHandlerChanged( m_Handler, value, this );
				m_Handler = value;
			}
		}

		public DateTime Sent
		{
			get
			{
				return m_Sent;
			}
		}

		public string Message
		{
			get
			{
				return m_Message;
			}
		}

		public PageType Type
		{
			get
			{
				return m_Type;
			}
		}

		public Point3D PageLocation
		{
			get
			{
				return m_PageLocation;
			}
		}

		public Map PageMap
		{
			get
			{
				return m_PageMap;
			}
		}

		public List<SpeechLogEntry> SpeechLog
		{
			get
			{
				return m_SpeechLog;
			}
		}

		private Timer m_Timer;

		public void Stop()
		{
			if ( m_Timer != null )
				m_Timer.Stop();

			m_Timer = null;
		}

		public void AddResponse( Mobile mob, string text )
		{
			if ( m_PageInfo != null )
			{
				lock ( m_PageInfo )
					m_PageInfo.Responses.Add( PageInfo.GetAccount( mob ), text );

				if ( PageInfo.ResFromResp( text ) != PageResolution.None )
					m_PageInfo.UpdateResolver();
			}
		}

		public PageEntry( Mobile sender, string message, PageType type )
		{
			m_Sender = sender;
			m_Sent = DateTime.Now;
			m_Message = Utility.FixHtml( message );
			m_Type = type;
			m_PageLocation = sender.Location;
			m_PageMap = sender.Map;

			PlayerMobile pm = sender as PlayerMobile;
			if ( pm != null && pm.SpeechLog != null && Array.IndexOf( SpeechLogAttachment, type ) >= 0 )
				m_SpeechLog = new List<SpeechLogEntry>( pm.SpeechLog );

			m_Timer = new InternalTimer( this );
			m_Timer.Start();

			StaffHistory history = Reports.Reports.StaffHistory;

			if ( history != null )
			{
				m_PageInfo = new PageInfo( this );

				history.AddPage( m_PageInfo );
			}
		}

		private class InternalTimer : Timer
		{
			private static TimeSpan StatusDelay = TimeSpan.FromMinutes( 2.0 );

			private PageEntry m_Entry;

			public InternalTimer( PageEntry entry ) : base( TimeSpan.FromSeconds( 1.0 ), StatusDelay )
			{
				m_Entry = entry;
			}

			protected override void OnTick()
			{
				int index = PageQueue.IndexOf( m_Entry );

				if ( m_Entry.Sender.NetState != null && index != -1 )
				{
					m_Entry.Sender.SendLocalizedMessage( 1008077, true, (index + 1).ToString() ); // Thank you for paging. Queue status : 
					m_Entry.Sender.SendLocalizedMessage( 1008084 ); // You can reference our website at www.uo.com or contact us at support@uo.com. To cancel your page, please select the help button again and select cancel.
				}
				else
				{
					if ( index != -1 )
						m_Entry.AddResponse( m_Entry.Sender, "[Logout]" );

					PageQueue.Remove( m_Entry );
				}
			}
		}
	}

	public class PageQueue
	{
		private static ArrayList m_List = new ArrayList();
		private static Hashtable m_KeyedByHandler = new Hashtable();
		private static Hashtable m_KeyedBySender = new Hashtable();

		public static void Initialize()
		{
			CommandSystem.Register( "Pages", AccessLevel.Counselor, new CommandEventHandler( Pages_OnCommand ) );
		}

		public static bool CheckAllowedToPage( Mobile from )
		{
			PlayerMobile pm = from as PlayerMobile;

			if ( pm == null )
				return true;

			if ( pm.DesignContext != null )
			{
				from.SendLocalizedMessage( 500182 ); // You cannot request help while customizing a house or transferring a character.
				return false;
			}
			else if ( pm.PagingSquelched )
			{
				from.SendMessage( "You cannot request help, sorry." );
				return false;
			}

			return true;
		}

		public static string GetPageTypeName( PageType type )
		{
			if ( type == PageType.VerbalHarassment )
				return "Verbal Harassment";
			else if ( type == PageType.PhysicalHarassment )
				return "Physical Harassment";
			else
				return type.ToString();
		}

		public static void OnHandlerChanged( Mobile old, Mobile value, PageEntry entry )
		{
			if ( old != null )
				m_KeyedByHandler.Remove( old );

			if ( value != null )
				m_KeyedByHandler[value] = entry;
		}

		[Usage( "Pages" )]
		[Description( "Opens the page queue menu." )]
		private static void Pages_OnCommand( CommandEventArgs e )
		{
			PageEntry entry = (PageEntry)m_KeyedByHandler[e.Mobile];

			if ( entry != null )
			{
				e.Mobile.SendGump( new PageEntryGump( e.Mobile, entry ) );
			}
			else if ( m_List.Count > 0 )
			{
				e.Mobile.SendGump( new PageQueueGump() );
			}
			else
			{
				e.Mobile.SendMessage( "The page queue is empty." );
			}
		}

		public static bool IsHandling( Mobile check )
		{
			return m_KeyedByHandler.ContainsKey( check );
		}

		public static bool Contains( Mobile sender )
		{
			return m_KeyedBySender.ContainsKey( sender );
		}

		public static int IndexOf( PageEntry e )
		{
			return m_List.IndexOf( e );
		}

		public static void Cancel( Mobile sender )
		{
			Remove( (PageEntry) m_KeyedBySender[sender] );
		}

		public static void Remove( PageEntry e )
		{
			if ( e == null )
				return;

			e.Stop();

			m_List.Remove( e );
			m_KeyedBySender.Remove( e.Sender );

			if ( e.Handler != null )
				m_KeyedByHandler.Remove( e.Handler );
		}

		public static PageEntry GetEntry( Mobile sender )
		{
			return (PageEntry)m_KeyedBySender[sender];
		}

		public static void Remove( Mobile sender )
		{
			Remove( GetEntry( sender ) );
		}

		public static ArrayList List
		{
			get
			{
				return m_List;
			}
		}

		public static void Enqueue( PageEntry entry )
		{
			m_List.Add( entry );
			m_KeyedBySender[entry.Sender] = entry;

			bool isStaffOnline = false;

			foreach ( NetState ns in NetState.Instances )
			{
				Mobile m = ns.Mobile;

				if ( m != null && m.AccessLevel >= AccessLevel.Counselor && m.AutoPageNotify && !IsHandling( m ) )
					m.SendMessage( "A new page has been placed in the queue." );

				if ( m != null && m.AccessLevel >= AccessLevel.Counselor && m.AutoPageNotify && m.LastMoveTime >= (DateTime.Now - TimeSpan.FromMinutes( 10.0 )) )
					isStaffOnline = true;
			}

			if ( !isStaffOnline )
				entry.Sender.SendMessage( "We are sorry, but no staff members are currently available to assist you.  Your page will remain in the queue until one becomes available, or until you cancel it manually." );

			if ( Email.SpeechLogPageAddresses != null && entry.SpeechLog != null )
				SendEmail( entry );
		}

		private static void SendEmail( PageEntry entry )
		{
			Mobile sender = entry.Sender;
			DateTime time = DateTime.Now;

			MailMessage mail = new MailMessage( "RunUO", Email.SpeechLogPageAddresses );

			mail.Subject = "RunUO Speech Log Page Forwarding";

			using ( StringWriter writer = new StringWriter() )
			{
				writer.WriteLine( "RunUO Speech Log Page - {0}", PageQueue.GetPageTypeName( entry.Type ) );
				writer.WriteLine();

				writer.WriteLine( "From: '{0}', Account: '{1}'", sender.RawName, sender.Account is Account ? sender.Account.Username : "???" );
				writer.WriteLine( "Location: {0} [{1}]", sender.Location, sender.Map );
				writer.WriteLine( "Sent on: {0}/{1:00}/{2:00} {3}:{4:00}:{5:00}", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second );
				writer.WriteLine();

				writer.WriteLine( "Message:" );
				writer.WriteLine( "'{0}'", entry.Message );
				writer.WriteLine();

				writer.WriteLine( "Speech Log" );
				writer.WriteLine( "==========" );

				foreach ( SpeechLogEntry logEntry in entry.SpeechLog )
				{
					Mobile from = logEntry.From;
					string fromName = from.RawName;
					string fromAccount = from.Account is Account ? from.Account.Username : "???";
					DateTime created = logEntry.Created;
					string speech = logEntry.Speech;

					writer.WriteLine( "{0}:{1:00}:{2:00} - {3} ({4}): '{5}'", created.Hour, created.Minute, created.Second, fromName, fromAccount, speech );
				}

				mail.Body = writer.ToString();
			}

			Email.AsyncSend( mail );
		}
	}
}