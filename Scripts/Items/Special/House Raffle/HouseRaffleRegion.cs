using System;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Spells.Sixth;
using Server.Targeting;

namespace Server.Regions
{
	public class HouseRaffleRegion : BaseRegion
	{
		private HouseRaffleStone m_Stone;

		public HouseRaffleRegion( HouseRaffleStone stone )
			: base( null, stone.PlotFacet, Region.DefaultPriority, stone.PlotBounds )
		{
			m_Stone = stone;
		}

		public bool CheckAccount( Mobile mobCheck, Mobile accCheck )
		{
			if ( accCheck != null )
			{
				Account a = accCheck.Account as Account;

				if ( a != null )
				{
					for ( int i = 0; i < a.Length; ++i )
					{
						if ( a[i] == mobCheck )
							return true;
					}
				}
			}

			return false;
		}

		public override bool AllowHousing( Mobile from, Point3D p )
		{
			if ( m_Stone == null || m_Stone.Winner == null )
				return false;

			return ( from == m_Stone.Winner || CheckAccount( from, m_Stone.Winner ) );
		}

		public override bool OnTarget( Mobile m, Target t, object o )
		{
			if ( m.Spell != null && m.Spell is MarkSpell && m.AccessLevel == AccessLevel.Player )
			{
				m.SendLocalizedMessage( 501800 ); // You cannot mark an object at that location.
				return false;
			}

			return base.OnTarget( m, t, o );
		}

		public override bool OnBeginSpellCast( Mobile m, ISpell s )
		{
			if ( s is MarkSpell && m.AccessLevel == AccessLevel.Player )
			{
				m.SendLocalizedMessage( 501800 ); // You cannot mark an object at that location.
				return false;
			}

			return base.OnBeginSpellCast( m, s );
		}
	}
}
