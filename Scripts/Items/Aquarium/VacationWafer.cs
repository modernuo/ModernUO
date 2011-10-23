using System;
using Server;
using Server.Mobiles;

namespace Server.Items
{
	public class VacationWafer : Item
	{		
		public override int LabelNumber{ get{ return 1074431; } } // An aquarium flake sphere
		
		[Constructable]
		public VacationWafer() : base( 0x971 )
		{
		}

		public VacationWafer( Serial serial ) : base( serial )
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
