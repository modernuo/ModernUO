using System;
using Server;

namespace Server.Items
{
	public class Ruby : Item
	{
		public override double DefaultWeight
		{
			get { return 0.1; }
		}

		[Constructable]
		public Ruby() : this( 1 )
		{
		}

		[Constructable]
		public Ruby( int amount ) : base( 0xF13 )
		{
			Stackable = true;
			Amount = amount;
		}

		public Ruby( Serial serial ) : base( serial )
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