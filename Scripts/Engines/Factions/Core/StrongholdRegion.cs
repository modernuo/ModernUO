using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Regions;

namespace Server.Factions
{
	public class StrongholdRegion : BaseRegion
	{
		private Faction m_Faction;

		public Faction Faction
		{
			get{ return m_Faction; }
			set{ m_Faction = value; }
		}

		public StrongholdRegion( Faction faction ) : base( faction.Definition.FriendlyName, Faction.Facet, Region.DefaultPriority, faction.Definition.Stronghold.Area )
		{
			m_Faction = faction;

			Register();
		}

		public override bool OnMoveInto( Mobile m, Direction d, Point3D newLocation, Point3D oldLocation )
		{
			if ( !base.OnMoveInto( m, d, newLocation, oldLocation ) )
				return false;

			if ( m.AccessLevel >= AccessLevel.Counselor || Contains( oldLocation ) )
				return true;
			
			if ( m is PlayerMobile ) {
				PlayerMobile pm = (PlayerMobile)m;

				if ( pm.DuelContext != null ) {
					m.SendMessage( "You may not enter this area while participating in a duel or a tournament." );
					return false;
				}
			}

			return ( Faction.Find( m, true, true ) != null );
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			return false;
		}
	}
}