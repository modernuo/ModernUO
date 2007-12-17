using System;
using Server;
using Server.Network;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
	public class WarningItem : Item
	{
		private string m_WarningString;
		private int m_WarningNumber;
		private int m_Range;
		private TimeSpan m_ResetDelay;

		[CommandProperty( AccessLevel.GameMaster )]
		public string WarningString
		{
			get{ return m_WarningString; }
			set{ m_WarningString = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int WarningNumber
		{
			get{ return m_WarningNumber; }
			set{ m_WarningNumber = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Range
		{
			get{ return m_Range; }
			set{ if ( value > 18 ) value = 18; m_Range = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan ResetDelay
		{
			get{ return m_ResetDelay; }
			set{ m_ResetDelay = value; }
		}

		[Constructable]
		public WarningItem( int itemID, int range, int warning ) : base( itemID )
		{
			if ( range > 18 )
				range = 18;

			Movable = false;

			m_WarningNumber = warning;
			m_Range = range;
		}

		[Constructable]
		public WarningItem( int itemID, int range, string warning ) : base( itemID )
		{
			if ( range > 18 )
				range = 18;

			Movable = false;

			m_WarningString = warning;
			m_Range = range;
		}

		public WarningItem( Serial serial ) : base( serial )
		{
		}

		private bool m_Broadcasting;

		private DateTime m_LastBroadcast;

		public virtual void SendMessage( Mobile triggerer, bool onlyToTriggerer, string messageString, int messageNumber )
		{
			if ( onlyToTriggerer )
			{
				if ( messageString != null )
					triggerer.SendMessage( messageString );
				else
					triggerer.SendLocalizedMessage( messageNumber );
			}
			else
			{
				if ( messageString != null )
					PublicOverheadMessage( MessageType.Regular, 0x3B2, false, messageString );
				else
					PublicOverheadMessage( MessageType.Regular, 0x3B2, messageNumber );
			}
		}

		public virtual bool OnlyToTriggerer{ get{ return false; } }
		public virtual int NeighborRange { get { return 5; } }

		public virtual void Broadcast( Mobile triggerer )
		{
			if ( m_Broadcasting || (DateTime.Now < (m_LastBroadcast + m_ResetDelay)) )
				return;

			m_LastBroadcast = DateTime.Now;

			m_Broadcasting = true;

			SendMessage( triggerer, this.OnlyToTriggerer, m_WarningString, m_WarningNumber );

			if ( NeighborRange >= 0 )
			{
				List<WarningItem> list = new List<WarningItem>();

				foreach ( Item item in GetItemsInRange( NeighborRange ) )
				{
					if ( item != this && item is WarningItem )
						list.Add( (WarningItem)item );
				}

				for ( int i = 0; i < list.Count; i++ )
					list[i].Broadcast( triggerer );
			}

			Timer.DelayCall( TimeSpan.Zero, new TimerCallback( InternalCallback ) );
		}

		private void InternalCallback()
		{
			m_Broadcasting = false;
		}

		public override bool HandlesOnMovement{ get{ return true; } }

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			if ( m.Player && Utility.InRange( m.Location, Location, m_Range ) && !Utility.InRange( oldLocation, Location, m_Range ) )
				Broadcast( m );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );

			writer.Write( (string) m_WarningString );
			writer.Write( (int) m_WarningNumber );
			writer.Write( (int) m_Range );

			writer.Write( (TimeSpan) m_ResetDelay );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_WarningString = reader.ReadString();
					m_WarningNumber = reader.ReadInt();
					m_Range = reader.ReadInt();
					m_ResetDelay = reader.ReadTimeSpan();

					break;
				}
			}
		}
	}

	public class HintItem : WarningItem
	{
		private string m_HintString;
		private int m_HintNumber;

		[CommandProperty( AccessLevel.GameMaster )]
		public string HintString
		{
			get{ return m_HintString; }
			set{ m_HintString = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int HintNumber
		{
			get{ return m_HintNumber; }
			set{ m_HintNumber = value; }
		}

		public override bool OnlyToTriggerer{ get{ return true; } }

		[Constructable]
		public HintItem( int itemID, int range, int warning, int hint ) : base( itemID, range, warning )
		{
			m_HintNumber = hint;
		}

		[Constructable]
		public HintItem( int itemID, int range, string warning, string hint ) : base( itemID, range, warning )
		{
			m_HintString = hint;
		}

		public HintItem( Serial serial ) : base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			SendMessage( from, true, m_HintString, m_HintNumber );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );

			writer.Write( (string) m_HintString );
			writer.Write( (int) m_HintNumber );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_HintString = reader.ReadString();
					m_HintNumber = reader.ReadInt();

					break;
				}
			}
		}
	}
}