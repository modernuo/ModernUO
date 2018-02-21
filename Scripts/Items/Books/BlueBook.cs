using System;
using Server;

namespace Server.Items
{
	public class BlueBook : BaseBook
	{

		[Constructible]
		public BlueBook() : base( 0xFF2, 40, true )
		{
		}

		[Constructible]
		public BlueBook( int pageCount, bool writable ) : base( 0xFF2, pageCount, writable )
		{
		}

		[Constructible]
		public BlueBook( string title, string author, int pageCount, bool writable ) : base( 0xFF2, title, author, pageCount, writable )
		{
		}

		// Intended for defined books only
		public BlueBook( bool writable ) : base( 0xFF2, writable )
		{
		}

		public BlueBook( Serial serial ) : base( serial )
		{
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}
	}
}
