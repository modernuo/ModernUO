using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Items
{
	public enum TalismanSlayerName
	{
		None,
		Bear,
		Vermin,
		Bat,
		Mage,
		Beetle,
		Bird,
		Ice,
		Flame,
		Bovine
	}

	public static class TalismanSlayer
	{
		private static Dictionary<TalismanSlayerName, Type[]> m_Table = new Dictionary<TalismanSlayerName,Type[]>();

		public static void Initialize()
		{
			m_Table[ TalismanSlayerName.Bear ] = new Type[]
			{
				typeof( GrizzlyBear ), typeof( BlackBear ), typeof( BrownBear ), typeof( PolarBear ) //, typeof( Grobu )
			};

			m_Table[ TalismanSlayerName.Vermin ] = new Type[]
			{
				typeof( RatmanMage ), typeof( RatmanMage ), typeof( RatmanArcher ), typeof( Barracoon),
				typeof( Ratman ), typeof( Sewerrat ), typeof( Rat ), typeof( GiantRat ) //, typeof( Chiikkaha )
			};

			m_Table[ TalismanSlayerName.Bat ] = new Type[]
			{
				typeof( Mongbat ), typeof( StrongMongbat ), typeof( VampireBat )
			};
			
			m_Table[ TalismanSlayerName.Mage ] = new Type[]
			{
				typeof( EvilMage ), typeof( EvilMageLord ), typeof( AncientLich ), typeof( Lich ), typeof( LichLord ),
				typeof( SkeletalMage ), typeof( BoneMagi ), typeof( OrcishMage ), typeof( KhaldunZealot ), typeof( JukaMage ),
			};

			m_Table[ TalismanSlayerName.Beetle ] = new Type[]
			{
				typeof( Beetle ), typeof( RuneBeetle ), typeof( FireBeetle ), typeof( DeathwatchBeetle ),
				typeof( DeathwatchBeetleHatchling )
			};

			m_Table[ TalismanSlayerName.Bird ] = new Type[]
			{
				typeof( Bird ), typeof( TropicalBird ), typeof( Chicken ), typeof( Crane ),
				typeof( DesertOstard ), typeof( Eagle ), typeof( ForestOstard ), typeof( FrenziedOstard ),
				typeof( Phoenix ), /*typeof( Pyre ), typeof( Swoop ), typeof( Saliva ),*/ typeof( Harpy ),
				typeof( StoneHarpy ) // ?????
			};

			m_Table[ TalismanSlayerName.Ice ] = new Type[]
			{ 
				typeof( ArcticOgreLord ), typeof( IceElemental ), typeof( SnowElemental ), typeof( FrostOoze ),
				typeof( IceFiend ), /*typeof( UnfrozenMummy ),*/ typeof( FrostSpider ), typeof( LadyOfTheSnow ),
				typeof( FrostTroll ),

				  // TODO WinterReaper, check
				typeof( IceSnake ), typeof( SnowLeopard ), typeof( PolarBear ),  typeof( IceSerpent ), typeof( GiantIceWorm )
			};
			
			m_Table[ TalismanSlayerName.Flame ] = new Type[]
			{
				typeof( FireBeetle ), typeof( HellHound ), typeof( LavaSerpent ), typeof( FireElemental ),
				typeof( PredatorHellCat ), typeof( Phoenix ), typeof( FireGargoyle ), typeof( HellCat ),
				/*typeof( Pyre ),*/ typeof( FireSteed ), typeof( LavaLizard ),

				// TODO check
				typeof( LavaSnake ),
			};

			m_Table[ TalismanSlayerName.Bovine ] = new Type[]
			{
				typeof( Cow ), typeof( Bull ), typeof( Gaman ) /*, typeof( MinotaurCaptain ),
				typeof( MinotaurScout ), typeof( Minotaur )*/

				// TODO TormentedMinotaur
			};
		}

		public static bool Slays( TalismanSlayerName name, Mobile m )
		{
			if ( !m_Table.ContainsKey( name ) )
				return false;

			Type[] types = m_Table[ name ];
			
			if ( types == null || m == null )
				return false;

			Type type = m.GetType();

			for ( int i = 0; i < types.Length; i++ )
			{
				if ( types[ i ].IsAssignableFrom( type ) )
					return true;
			}

			return false;
		}
	}
}
