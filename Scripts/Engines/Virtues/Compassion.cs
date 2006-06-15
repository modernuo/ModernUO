using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;

namespace Server
{
	public class CompassionVirtue
	{
		private static TimeSpan LossDelay = TimeSpan.FromDays( 7.0 );
		private const int LossAmount = 500;

		public static void Initialize()
		{
			VirtueGump.Register( 105, new OnVirtueUsed( OnVirtueUsed ) );
		}

		public static void OnVirtueUsed( Mobile from )
		{
			from.SendLocalizedMessage( 1053001 ); // This virtue is not activated through the virtue menu.
		}

		public static void CheckAtrophy( Mobile from )
		{
			PlayerMobile pm = from as PlayerMobile;

			if ( pm == null )
				return;

			try
			{
				if ( (pm.LastCompassionLoss + LossDelay) < DateTime.Now )
				{
					VirtueHelper.Atrophy( from, VirtueName.Compassion, LossAmount );
					//OSI has no cliloc message for losing compassion.  Weird.
					pm.LastCompassionLoss = DateTime.Now;
				}
			}
			catch
			{
			}
		}
	}
}