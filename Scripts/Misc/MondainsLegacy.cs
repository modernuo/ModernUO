using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server
{
	public static class MondainsLegacy
	{
		public static Type[] Artifacts { get { return m_Artifacts; } }

		private static Type[] m_Artifacts = new Type[]
		{
			typeof( AegisOfGrace ), typeof( BladeDance ), typeof( BloodwoodSpirit ), typeof( Bonesmasher ),
			typeof( Boomstick ), typeof( BrightsightLenses ), typeof( FeyLeggings ), typeof( FleshRipper ),
			typeof( HelmOfSwiftness ), typeof( PadsOfTheCuSidhe ), typeof( QuiverOfRage ), typeof( QuiverOfElements ),
			typeof( RaedsGlory ), typeof( RighteousAnger ), typeof( RobeOfTheEclipse ), typeof( RobeOfTheEquinox ),
			typeof( SoulSeeker ), typeof( TalonBite ), typeof( TotemOfVoid ), typeof( WildfireBow ),
			typeof( Windsong )
		};

		public static bool CheckArtifactChance( Mobile m, BaseCreature bc )
		{
			if ( !Core.ML )
				return false;

			return Paragon.CheckArtifactChance( m, bc );
		}

		public static void GiveArtifactTo( Mobile m )
		{
			Item item = Activator.CreateInstance( m_Artifacts[Utility.Random( m_Artifacts.Length )] ) as Item;

			if ( item == null )
				return;

			if ( m.AddToBackpack( item ) )
			{
				m.SendLocalizedMessage( 1072223 ); // An item has been placed in your backpack.
				m.SendLocalizedMessage( 1062317 ); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
			}
			else if ( m.BankBox.TryDropItem( m, item, false ) )
			{
				m.SendLocalizedMessage( 1072224 ); // An item has been placed in your bank box.
				m.SendLocalizedMessage( 1062317 ); // For your valor in combating the fallen beast, a special artifact has been bestowed on you.
			}
			else
			{
				// Item was placed at feet by m.AddToBackpack
				m.SendLocalizedMessage( 1072523 ); // You find an artifact, but your backpack and bank are too full to hold it.
			}
		}

		public static bool CheckML( Mobile from )
		{
			return CheckML( from, true );
		}

		public static bool CheckML( Mobile from, bool message )
		{
			if ( from == null || from.NetState == null )
				return false;

			if ( from.NetState.SupportsExpansion( Expansion.ML ) )
				return true;

			if ( message )
				from.SendLocalizedMessage( 1072791 ); // You must upgrade to Mondain's Legacy in order to use that item.

			return false;
		}

		public static bool IsMLRegion( Region region )
		{
			return region.IsPartOf( "Twisted Weald" )
				|| region.IsPartOf( "Sanctuary" )
				|| region.IsPartOf( "The Prism of Light" )
				|| region.IsPartOf( "The Citadel" )
				|| region.IsPartOf( "Bedlam" )
				|| region.IsPartOf( "Blighted Grove" )
				|| region.IsPartOf( "The Painted Caves" )
				|| region.IsPartOf( "The Palace of Paroxysmus" )
				|| region.IsPartOf( "Labyrinth" );
		}
	}
}