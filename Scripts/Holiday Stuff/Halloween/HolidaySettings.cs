using System;
using Server.Items;

namespace Server.Events.Halloween
{
	class HolidaySettings
	{
		public static DateTime StartHalloween // YY MM DD
			=> new DateTime( 2012, 10, 24 );

		public static DateTime FinishHalloween => new DateTime( 2012, 11, 15 );

		public static Item RandomGMBeggerItem => ( Item )Activator.CreateInstance( m_GMBeggarTreats[ Utility.Random( m_GMBeggarTreats.Length ) ] );
		public static Item RandomTreat => (Item)Activator.CreateInstance ( m_Treats[ Utility.Random( m_Treats.Length ) ]);

		private static Type[] m_GMBeggarTreats =
		{
				  typeof( CreepyCake ),
				  typeof( PumpkinPizza ),
				  typeof( GrimWarning ),
				  typeof( HarvestWine ),
				  typeof( MurkyMilk ),
				  typeof( MrPlainsCookies ),
				  typeof( SkullsOnPike ),
				  typeof( ChairInAGhostCostume ),
				  typeof( ExcellentIronMaiden ),
				  typeof( HalloweenGuillotine ),
				  typeof( ColoredSmallWebs )
		};

		private static Type[] m_Treats =
		{
				  typeof( Lollipops ),
				  typeof( WrappedCandy ),
				  typeof( JellyBeans ),
				  typeof( Taffy ),
				  typeof( NougatSwirl )
		};
	}
}
