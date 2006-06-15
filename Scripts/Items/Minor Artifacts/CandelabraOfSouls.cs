using System;
using Server;

namespace Server.Items
{
	public class CandelabraOfSouls : Item
	{
		public override int LabelNumber{ get{ return 1063478; } }
		
		[Constructable]
		public CandelabraOfSouls() : base( 0xB26 )
		{
		}

		public CandelabraOfSouls( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}