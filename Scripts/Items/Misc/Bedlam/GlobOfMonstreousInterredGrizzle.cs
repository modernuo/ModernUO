using System;
using Server;

namespace Server.Items
{
	public class GlobOfMonstreousInterredGrizzle : Item
	{
		public override int LabelNumber{ get{ return 1072117; } } // Glob of Monsterous Interred Grizzle

		[Constructable]
		public GlobOfMonstreousInterredGrizzle() : base( 0x2F3 )
		{
		}

		public GlobOfMonstreousInterredGrizzle( Serial serial ) : base( serial )
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

