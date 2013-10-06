using System;
using Server.Mobiles;

namespace Server
{
	public enum VirtueLevel
	{
		None,
		Seeker,
		Follower,
		Knight
	}

	public enum VirtueName
	{
		Humility,
		Sacrifice,
		Compassion,
		Spirituality,
		Valor,
		Honor,
		Justice,
		Honesty
	}

	public class VirtueHelper
	{
		public static bool HasAny( Mobile from, VirtueName virtue )
		{
			return ( from.Virtues.GetValue( (int)virtue ) > 0 );
		}

		public static bool IsHighestPath( Mobile from, VirtueName virtue )
		{
			return ( from.Virtues.GetValue( (int)virtue ) >= GetMaxAmount( virtue ) );
		}

		public static VirtueLevel GetLevel( Mobile from, VirtueName virtue )
		{
			int v = from.Virtues.GetValue( (int)virtue );
			int vl;
			int vmax = GetMaxAmount( virtue );

			if ( v < 4000 )
				vl = 0;
			else if ( v >= vmax)
				vl = 3;
			else
				vl = ( v + 9999 ) / 10000;

			return (VirtueLevel)vl;
		}

		public static int GetMaxAmount( VirtueName virtue )
		{
			if( virtue == VirtueName.Honor )
				return 20000;

			if( virtue == VirtueName.Sacrifice )
				return 22000;

			return 21000;
		}

		public static bool Award( Mobile from, VirtueName virtue, int amount, ref bool gainedPath )
		{
			int current = from.Virtues.GetValue( (int)virtue );

			int maxAmount = GetMaxAmount( virtue );

			if ( current >= maxAmount )
				return false;

			if( (current + amount) >= maxAmount )
				amount = maxAmount - current;

			VirtueLevel oldLevel = GetLevel( from, virtue );

			from.Virtues.SetValue( (int)virtue, current + amount );

			gainedPath = ( GetLevel( from, virtue ) != oldLevel );

			return true;
		}

		public static bool Atrophy( Mobile from, VirtueName virtue )
		{
			return Atrophy( from, virtue, 1 );
		}

		public static bool Atrophy( Mobile from, VirtueName virtue, int amount )
		{
			int current = from.Virtues.GetValue( (int)virtue );

			if( (current - amount) >= 0 )
				from.Virtues.SetValue( (int)virtue, current - amount );
			else
				from.Virtues.SetValue( (int)virtue, 0 );

			return ( current > 0 );
		}

		public static bool IsSeeker( Mobile from, VirtueName virtue )
		{
			return ( GetLevel( from, virtue ) >= VirtueLevel.Seeker );
		}

		public static bool IsFollower( Mobile from, VirtueName virtue )
		{
			return ( GetLevel( from, virtue ) >= VirtueLevel.Follower );
		}

		public static bool IsKnight( Mobile from, VirtueName virtue )
		{
			return ( GetLevel( from, virtue ) >= VirtueLevel.Knight );
		}

		public static void AwardVirtue( PlayerMobile pm, VirtueName virtue, int amount )
		{
			if ( virtue == VirtueName.Compassion )
			{
				if ( pm.CompassionGains > 0 && DateTime.UtcNow > pm.NextCompassionDay )
				{
					pm.NextCompassionDay = DateTime.MinValue;
					pm.CompassionGains = 0;
				}

				if ( pm.CompassionGains >= 5 )
				{
					pm.SendLocalizedMessage( 1053004 ); // You must wait about a day before you can gain in compassion again.
					return;
				}
			}

			bool gainedPath = false;
			string virtueName = Enum.GetName( typeof( VirtueName ), virtue );

			if ( VirtueHelper.Award( pm, virtue, amount, ref gainedPath ) )
			{
				// TODO: Localize?
				if ( gainedPath )
					pm.SendMessage( "You have gained a path in {0}!", virtueName );
				else
					pm.SendMessage( "You have gained in {0}.", virtueName );

				if ( virtue == VirtueName.Compassion )
				{
					pm.NextCompassionDay = DateTime.UtcNow + TimeSpan.FromDays( 1.0 );
					++pm.CompassionGains;

					if ( pm.CompassionGains >= 5 )
						pm.SendLocalizedMessage( 1053004 ); // You must wait about a day before you can gain in compassion again.
				}
			}
			else
			{
				// TODO: Localize?
				pm.SendMessage( "You have achieved the highest path of {0} and can no longer gain any further.", virtueName );
			}
		}
	}
}
