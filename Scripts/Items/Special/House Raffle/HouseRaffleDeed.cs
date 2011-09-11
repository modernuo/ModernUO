using System;
using Server;
using Server.Misc;

namespace Server.Items
{
	public class HouseRaffleDeed : Item
	{
		private Point3D m_PlotLocation;
		private Map m_Facet;
		private Mobile m_AwardedTo;

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public Point3D PlotLocation
		{
			get { return m_PlotLocation; }
			set { m_PlotLocation = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public Map PlotFacet
		{
			get { return m_Facet; }
			set { m_Facet = value; InvalidateProperties(); }
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Seer )]
		public Mobile AwardedTo
		{
			get { return m_AwardedTo; }
			set { m_AwardedTo = value; InvalidateProperties(); }
		}

		public override string DefaultName
		{
			get { return "a writ of lease"; }
		}

		public override double DefaultWeight
		{
			get { return 1.0; }
		}

		[Constructable]
		public HouseRaffleDeed()
			: this ( Point3D.Zero, null, null )
		{
		}

		public HouseRaffleDeed( Point3D loc, Map facet, Mobile m ) : base( 0x2830 )
		{
			m_PlotLocation = loc;
			m_Facet = facet;
			m_AwardedTo = m;

			LootType = LootType.Blessed;
			Hue = 0x501;
		}

		public HouseRaffleDeed( Serial serial )
			: base( serial )
		{
		}

		public bool ValidLocation()
		{
			return ( m_PlotLocation != Point3D.Zero && m_Facet != null && m_Facet != Map.Internal );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( ValidLocation() )
			{
				list.Add( 1060658, "location\t{0}", HouseRaffleStone.FormatLocation( m_PlotLocation, m_Facet, false ) ); // ~1_val~: ~2_val~
				list.Add( 1060659, "facet\t{0}", m_Facet ); // ~1_val~: ~2_val~
			}

			//list.Add( 1060660, "shard\t{0}", ServerList.ServerName ); // ~1_val~: ~2_val~
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_PlotLocation );
			writer.Write( m_Facet );
			writer.Write( m_AwardedTo );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_PlotLocation = reader.ReadPoint3D();
					m_Facet = reader.ReadMap();
					m_AwardedTo = reader.ReadMobile();

					break;
				}
			}
		}
	}
}
