using System;

namespace Server.Items
{
	public class TaintedMushroom : Item
	{		
		public override int LabelNumber{ get{ return 1075088; } } // Dread Horn Tainted Mushroom
		public override bool ForceShowProperties{ get{ return true; } }
		
		private static int[] m_ItemIDs = new int[]
		{
			0x222E, 0x222F, 0x2230, 0x2231
		};
	
		[Constructable]
		public TaintedMushroom() : base( Utility.RandomList( m_ItemIDs ) )
		{		
		}

		public TaintedMushroom( Serial serial ) : base( serial )
		{
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

