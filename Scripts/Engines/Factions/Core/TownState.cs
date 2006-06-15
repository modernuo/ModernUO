using System;

namespace Server.Factions
{
	public class TownState
	{
		private Town m_Town;
		private Faction m_Owner;

		private Mobile m_Sheriff;
		private Mobile m_Finance;

		private int m_Silver;
		private int m_Tax;

		private DateTime m_LastTaxChange;
		private DateTime m_LastIncome;

		public Town Town
		{
			get{ return m_Town; }
			set{ m_Town = value; }
		}

		public Faction Owner
		{
			get{ return m_Owner; }
			set{ m_Owner = value; }
		}

		public Mobile Sheriff
		{
			get{ return m_Sheriff; }
			set
			{
				if ( m_Sheriff != null )
				{
					PlayerState pl = PlayerState.Find( m_Sheriff );

					if ( pl != null )
						pl.Sheriff = null;
				}

				m_Sheriff = value;

				if ( m_Sheriff != null )
				{
					PlayerState pl = PlayerState.Find( m_Sheriff );

					if ( pl != null )
						pl.Sheriff = m_Town;
				}
			}
		}

		public Mobile Finance
		{
			get{ return m_Finance; }
			set
			{
				if ( m_Finance != null )
				{
					PlayerState pl = PlayerState.Find( m_Finance );

					if ( pl != null )
						pl.Finance = null;
				}

				m_Finance = value;

				if ( m_Finance != null )
				{
					PlayerState pl = PlayerState.Find( m_Finance );

					if ( pl != null )
						pl.Finance = m_Town;
				}
			}
		}

		public int Silver
		{
			get{ return m_Silver; }
			set{ m_Silver = value; }
		}

		public int Tax
		{
			get{ return m_Tax; }
			set{ m_Tax = value; }
		}

		public DateTime LastTaxChange
		{
			get{ return m_LastTaxChange; }
			set{ m_LastTaxChange = value; }
		}

		public DateTime LastIncome
		{
			get{ return m_LastIncome; }
			set{ m_LastIncome = value; }
		}

		public TownState( Town town )
		{
			m_Town = town;
		}

		public TownState( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 3:
				{
					m_LastIncome = reader.ReadDateTime();

					goto case 2;
				}
				case 2:
				{
					m_Tax = reader.ReadEncodedInt();
					m_LastTaxChange = reader.ReadDateTime();

					goto case 1;
				}
				case 1:
				{
					m_Silver = reader.ReadEncodedInt();

					goto case 0;
				}
				case 0:
				{
					m_Town = Town.ReadReference( reader );
					m_Owner = Faction.ReadReference( reader );

					m_Sheriff = reader.ReadMobile();
					m_Finance = reader.ReadMobile();

					m_Town.State = this;

					break;
				}
			}
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 3 ); // version

			writer.Write( (DateTime) m_LastIncome );

			writer.WriteEncodedInt( (int) m_Tax );
			writer.Write( (DateTime) m_LastTaxChange );

			writer.WriteEncodedInt( (int) m_Silver );

			Town.WriteReference( writer, m_Town );
			Faction.WriteReference( writer, m_Owner );

			writer.Write( (Mobile) m_Sheriff );
			writer.Write( (Mobile) m_Finance );
		}
	}
}