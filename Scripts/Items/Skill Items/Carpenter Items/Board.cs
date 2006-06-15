using System;

namespace Server.Items
{
	[FlipableAttribute( 0x1BD7, 0x1BDA )]
	public class Board : Item, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( Amount == 1 ? "{0} board" : "{0} boards", Amount );
			}
		}

		[Constructable]
		public Board()
			: this( 1 )
		{
		}

		[Constructable]
		public Board( int amount )
			: base( 0x1BD7 )
		{
			Stackable = true;
			Amount = amount;
		}

		public Board( Serial serial )
			: base( serial )
		{
		}

		

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version == 0 && Weight == 0.1 )
				Weight = -1;
		}
	}
}