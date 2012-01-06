using System;
using Server;

namespace Server.Items
{
	public class MelisandesFermentedWine : GreaterExplosionPotion
	{
		public override int LabelNumber{ get{ return 1072114; } } // Melisande's Fermented Wine
		
		private static int[] m_Hues = new int[]
		{
			0xB, 0xF, 0x48D // TODO update
		};
	
		[Constructable]
		public MelisandesFermentedWine() : base()
		{
			Stackable = false;
			ItemID = 0x99B;
			Hue = Utility.RandomList( m_Hues );
		}

		public MelisandesFermentedWine( Serial serial ) : base( serial )
		{
		}
		
		public override void Drink( Mobile from )
		{
			if ( from.NetState.SupportsExpansion( Expansion.ML ) )
				base.Drink( from );
			else
				from.SendLocalizedMessage( 1072791 ); // You must upgrade to Mondain's Legacy in order to use that item.
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			list.Add( 1074502 ); // It looks explosive.
			list.Add( 1075085 ); // Requirement: Mondain's Legacy
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			
			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			
			int version = reader.ReadInt();
		}
	}
}


