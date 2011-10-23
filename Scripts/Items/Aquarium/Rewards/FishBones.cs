using System;
using Server;

namespace Server.Items
{
	public class FishBones : Item
	{		
		public override int LabelNumber{ get{ return 1074601; } } // Fish bones
		
		[Constructable]
		public FishBones() : base( 0x3B0C )
		{
			Weight = 1;
		}

		public FishBones( Serial serial ) : base( serial )
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
