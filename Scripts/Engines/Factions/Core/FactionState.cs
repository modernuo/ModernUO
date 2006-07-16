using System;
using System.Collections.Generic;

namespace Server.Factions
{
	public class FactionState
	{
		private Faction m_Faction;
		private Mobile m_Commander;
		private int m_Tithe;
		private int m_Silver;
		private List<PlayerState> m_Members;
		private Election m_Election;
		private List<FactionItem> m_FactionItems;
		private List<BaseFactionTrap> m_FactionTraps;

		private const int BroadcastsPerPeriod = 2;
		private static readonly TimeSpan BroadcastPeriod = TimeSpan.FromHours( 1.0 );

		private DateTime[] m_LastBroadcasts = new DateTime[BroadcastsPerPeriod];

		public bool FactionMessageReady
		{
			get
			{
				for ( int i = 0; i < m_LastBroadcasts.Length; ++i )
				{
					if ( DateTime.Now >= (m_LastBroadcasts[i] + BroadcastPeriod) )
						return true;
				}

				return false;
			}
		}

		public void RegisterBroadcast()
		{
			for ( int i = 0; i < m_LastBroadcasts.Length; ++i )
			{
				if ( DateTime.Now >= (m_LastBroadcasts[i] + BroadcastPeriod) )
				{
					m_LastBroadcasts[i] = DateTime.Now;
					break;
				}
			}
		}

		public List<FactionItem> FactionItems
		{
			get{ return m_FactionItems; }
			set{ m_FactionItems = value; }
		}

		public List<BaseFactionTrap> Traps
		{
			get{ return m_FactionTraps; }
			set{ m_FactionTraps = value; }
		}

		public Election Election
		{
			get{ return m_Election; }
			set{ m_Election = value; }
		}

		public Mobile Commander
		{
			get{ return m_Commander; }
			set
			{
				if ( m_Commander != null )
					m_Commander.InvalidateProperties();

				m_Commander = value;

				if ( m_Commander != null )
				{
					m_Commander.SendLocalizedMessage( 1042227 ); // You have been elected Commander of your faction

					m_Commander.InvalidateProperties();

					PlayerState pl = PlayerState.Find( m_Commander );

					if ( pl != null && pl.Finance != null )
						pl.Finance.Finance = null;

					if ( pl != null && pl.Sheriff != null )
						pl.Sheriff.Sheriff = null;
				}
			}
		}

		public int Tithe
		{
			get{ return m_Tithe; }
			set{ m_Tithe = value; }
		}

		public int Silver
		{
			get{ return m_Silver; }
			set{ m_Silver = value; }
		}

		public List<PlayerState> Members
		{
			get{ return m_Members; }
			set{ m_Members = value; }
		}

		public FactionState( Faction faction )
		{
			m_Faction = faction;
			m_Tithe = 50;
			m_Members = new List<PlayerState>();
			m_Election = new Election( faction );
			m_FactionItems = new List<FactionItem>();
			m_FactionTraps = new List<BaseFactionTrap>();
		}

		public FactionState( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 4:
				{
					int count = reader.ReadEncodedInt();

					for ( int i = 0; i < count; ++i )
					{
						DateTime time = reader.ReadDateTime();

						if ( i < m_LastBroadcasts.Length )
							m_LastBroadcasts[i] = time;
					}

					goto case 3;
				}
				case 3:
				case 2:
				case 1:
				{
					m_Election = new Election( reader );

					goto case 0;
				}
				case 0:
				{
					m_Faction = Faction.ReadReference( reader );

					m_Commander = reader.ReadMobile();

					if ( version < 4 )
					{
						DateTime time = reader.ReadDateTime();

						if ( m_LastBroadcasts.Length > 0 )
							m_LastBroadcasts[0] = time;
					}

					m_Tithe = reader.ReadEncodedInt();
					m_Silver = reader.ReadEncodedInt();

					int memberCount = reader.ReadEncodedInt();

					m_Members = new List<PlayerState>();

					for ( int i = 0; i < memberCount; ++i )
					{
						PlayerState pl = new PlayerState( reader, m_Faction, m_Members );

						if ( pl.Mobile != null )
							m_Members.Add( pl );
					}

					m_Faction.State = this;
					m_Faction.UpdateRanks();

					m_FactionItems = new List<FactionItem>();

					if ( version >= 2 )
					{
						int factionItemCount = reader.ReadEncodedInt();

						for ( int i = 0; i < factionItemCount; ++i )
						{
							FactionItem factionItem = new FactionItem( reader, m_Faction );

							if ( !factionItem.HasExpired )
								factionItem.Attach();
							else
								Timer.DelayCall( TimeSpan.Zero, new TimerCallback( factionItem.Detach ) ); // sandbox detachment
						}
					}

					m_FactionTraps = new List<BaseFactionTrap>();

					if ( version >= 3 )
					{
						int factionTrapCount = reader.ReadEncodedInt();

						for ( int i = 0; i < factionTrapCount; ++i )
						{
							BaseFactionTrap trap = reader.ReadItem() as BaseFactionTrap;

							if ( trap != null && !trap.CheckDecay() )
								m_FactionTraps.Add( trap );
						}
					}

					break;
				}
			}

			if ( version < 1 )
				m_Election = new Election( m_Faction );
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 4 ); // version

			writer.WriteEncodedInt( (int) m_LastBroadcasts.Length );

			for ( int i = 0; i < m_LastBroadcasts.Length; ++i )
				writer.Write( (DateTime) m_LastBroadcasts[i] );

			m_Election.Serialize( writer );

			Faction.WriteReference( writer, m_Faction );

			writer.Write( (Mobile) m_Commander );

			writer.WriteEncodedInt( (int) m_Tithe );
			writer.WriteEncodedInt( (int) m_Silver );

			writer.WriteEncodedInt( (int) m_Members.Count );

			for ( int i = 0; i < m_Members.Count; ++i )
			{
				PlayerState pl = (PlayerState) m_Members[i];

				pl.Serialize( writer );
			}

			writer.WriteEncodedInt( (int) m_FactionItems.Count );

			for ( int i = 0; i < m_FactionItems.Count; ++i )
				m_FactionItems[i].Serialize( writer );

			writer.WriteEncodedInt( (int) m_FactionTraps.Count );

			for ( int i = 0; i < m_FactionTraps.Count; ++i )
				writer.Write( (Item) m_FactionTraps[i] );
		}
	}
}