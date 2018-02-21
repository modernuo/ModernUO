using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class LichFormScroll : SpellScroll
	{
		[Constructible]
		public LichFormScroll() : this( 1 )
		{
		}

		[Constructible]
		public LichFormScroll( int amount ) : base( 106, 0x2266, amount )
		{
		}

		public LichFormScroll( Serial serial ) : base( serial )
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