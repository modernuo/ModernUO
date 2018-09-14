using System;

namespace Server.Factions
{
	public class TownState
	{
		private Mobile m_Sheriff;
		private Mobile m_Finance;

		public Town Town { get; set; }

		public Faction Owner { get; set; }

		public Mobile Sheriff
		{
			get => m_Sheriff;
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
						pl.Sheriff = Town;
				}
			}
		}

		public Mobile Finance
		{
			get => m_Finance;
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
						pl.Finance = Town;
				}
			}
		}

		public int Silver { get; set; }

		public int Tax { get; set; }

		public DateTime LastTaxChange { get; set; }

		public DateTime LastIncome { get; set; }

		public TownState( Town town )
		{
			Town = town;
		}

		public TownState( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 3:
				{
					LastIncome = reader.ReadDateTime();

					goto case 2;
				}
				case 2:
				{
					Tax = reader.ReadEncodedInt();
					LastTaxChange = reader.ReadDateTime();

					goto case 1;
				}
				case 1:
				{
					Silver = reader.ReadEncodedInt();

					goto case 0;
				}
				case 0:
				{
					Town = Town.ReadReference( reader );
					Owner = Faction.ReadReference( reader );

					m_Sheriff = reader.ReadMobile();
					m_Finance = reader.ReadMobile();

					Town.State = this;

					break;
				}
			}
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 3 ); // version

			writer.Write( (DateTime) LastIncome );

			writer.WriteEncodedInt( (int) Tax );
			writer.Write( (DateTime) LastTaxChange );

			writer.WriteEncodedInt( (int) Silver );

			Town.WriteReference( writer, Town );
			Faction.WriteReference( writer, Owner );

			writer.Write( (Mobile) m_Sheriff );
			writer.Write( (Mobile) m_Finance );
		}
	}
}