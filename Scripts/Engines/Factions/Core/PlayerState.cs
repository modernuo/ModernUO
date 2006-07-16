using System;
using Server;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Factions
{
	public class PlayerState : IComparable
	{
		private Mobile m_Mobile;
		private Faction m_Faction;
		private List<PlayerState> m_Owner;
		private int m_KillPoints;
		private DateTime m_Leaving;
		private MerchantTitle m_MerchantTitle;
		private RankDefinition m_Rank;
		private List<SilverGivenEntry> m_SilverGiven;

		private Town m_Sheriff;
		private Town m_Finance;

		public Mobile Mobile{ get{ return m_Mobile; } }
		public Faction Faction{ get{ return m_Faction; } }
		public List<PlayerState> Owner { get { return m_Owner; } }
		public int KillPoints{ get{ return m_KillPoints; } set{ m_KillPoints = value; } }
		public MerchantTitle MerchantTitle{ get{ return m_MerchantTitle; } set{ m_MerchantTitle = value; Invalidate(); } }
		public RankDefinition Rank{ get{ return m_Rank; } set{ m_Rank = value; Invalidate(); } }
		public Town Sheriff{ get{ return m_Sheriff; } set{ m_Sheriff = value; Invalidate(); } }
		public Town Finance{ get{ return m_Finance; } set{ m_Finance = value; Invalidate(); } }
		public List<SilverGivenEntry> SilverGiven { get { return m_SilverGiven; } }

		public DateTime Leaving{ get{ return m_Leaving; } set{ m_Leaving = value; } }
		public bool IsLeaving{ get{ return ( m_Leaving > DateTime.MinValue ); } }

		public bool CanGiveSilverTo( Mobile mob )
		{
			if ( m_SilverGiven == null )
				return true;

			for ( int i = 0; i < m_SilverGiven.Count; ++i )
			{
				SilverGivenEntry sge = m_SilverGiven[i];

				if ( sge.IsExpired )
					m_SilverGiven.RemoveAt( i-- );
				else if ( sge.GivenTo == mob )
					return false;
			}

			return true;
		}

		public void OnGivenSilverTo( Mobile mob )
		{
			if ( m_SilverGiven == null )
				m_SilverGiven = new List<SilverGivenEntry>();

			m_SilverGiven.Add( new SilverGivenEntry( mob ) );
		}

		public void Invalidate()
		{
			if ( m_Mobile is PlayerMobile )
				((PlayerMobile)m_Mobile).InvalidateProperties();
		}

		public void Attach()
		{
			if ( m_Mobile is PlayerMobile )
				((PlayerMobile)m_Mobile).FactionPlayerState = this;
		}

		public PlayerState( Mobile mob, Faction faction, List<PlayerState> owner )
		{
			m_Mobile = mob;
			m_Faction = faction;
			m_Owner = owner;

			m_Rank = faction.Definition.Ranks[faction.Definition.Ranks.Length - 1];

			Attach();
			Invalidate();
		}

		public PlayerState( GenericReader reader, Faction faction, List<PlayerState> owner )
		{
			m_Faction = faction;
			m_Owner = owner;

			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 0:
				{
					m_Mobile = reader.ReadMobile();

					m_KillPoints = reader.ReadEncodedInt();
					m_MerchantTitle = (MerchantTitle)reader.ReadEncodedInt();

					m_Leaving = reader.ReadDateTime();

					break;
				}
			}

			Attach();
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( (Mobile) m_Mobile );

			writer.WriteEncodedInt( (int) m_KillPoints );
			writer.WriteEncodedInt( (int) m_MerchantTitle );

			writer.Write( (DateTime) m_Leaving );
		}

		public static PlayerState Find( Mobile mob )
		{
			if ( mob is PlayerMobile )
				return ((PlayerMobile)mob).FactionPlayerState;

			return null;
		}

		public int CompareTo( object obj )
		{
			return m_KillPoints - ((PlayerState)obj).m_KillPoints;
		}
	}
}