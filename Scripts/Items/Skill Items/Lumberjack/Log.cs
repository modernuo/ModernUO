using System;
using Server.Items;

namespace Server.Items
{
	[FlipableAttribute( 0x1bdd, 0x1be0 )]
	public class Log : Item, ICommodity
	{
		string ICommodity.Description
		{
			get
			{
				return String.Format( Amount == 1 ? "{0} log" : "{0} logs", Amount );
			}
		}

		[Constructable]
		public Log() : this( 1 )
		{
		}

		[Constructable]
		public Log( int amount ) : base( 0x1BDD )
		{
			Stackable = true;
			Weight = 2.0;
			Amount = amount;
		}

		public Log( Serial serial ) : base( serial )
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