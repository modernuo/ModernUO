using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class ResurrectionScroll : SpellScroll
	{
		[Constructible]
		public ResurrectionScroll() : this( 1 )
		{
		}

		[Constructible]
		public ResurrectionScroll( int amount ) : base( 58, 0x1F67, amount )
		{
		}

		public ResurrectionScroll( Serial serial ) : base( serial )
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