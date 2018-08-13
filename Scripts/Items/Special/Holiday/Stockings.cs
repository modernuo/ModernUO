using System;
using Server;

namespace Server.Items
{
	[Furniture]
	[FlippableAttribute( 0x2bd9, 0x2bda )]
	public class GreenStocking : BaseContainer
	{
		public override int DefaultGumpID => 0x103;
		public override int DefaultDropSound => 0x42;

		[Constructible]
		public GreenStocking() : base ( Utility.Random( 0x2BD9, 2 ) )
		{
		}

		public GreenStocking( Serial serial ) : base( serial )
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

	[Furniture]
	[FlippableAttribute( 0x2bdb, 0x2bdc )]
	public class RedStocking : BaseContainer
	{
		public override int DefaultGumpID => 0x103;
		public override int DefaultDropSound => 0x42;

		[Constructible]
		public RedStocking() : base ( Utility.Random( 0x2BDB, 2 ) )
		{
		}

		public RedStocking( Serial serial ) : base( serial )
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

