using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class CraftysFishingHat : BaseHat
	{		
		public override int LabelNumber{ get{ return 1074572; } } // Crafty's Fishing Hat
		
		[Constructable]
		public CraftysFishingHat() : base( 0x1713 )
		{
		}

		public CraftysFishingHat( Serial serial ) : base( serial )
		{		
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			list.Add( 1073634 ); // An aquarium decoration
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
