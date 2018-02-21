using System;
using Server.Network;

namespace Server.Items
{
	[FlippableAttribute( 0xc77, 0xc78 )]
	public class Carrot : Food
	{
		[Constructible]
		public Carrot() : this( 1 )
		{
		}

		[Constructible]
		public Carrot( int amount ) : base( amount, 0xc78 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Carrot( Serial serial ) : base( serial )
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

	[FlippableAttribute( 0xc7b, 0xc7c )]
	public class Cabbage : Food
	{
		[Constructible]
		public Cabbage() : this( 1 )
		{
		}

		[Constructible]
		public Cabbage( int amount ) : base( amount, 0xc7b )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Cabbage( Serial serial ) : base( serial )
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

	[FlippableAttribute( 0xc6d, 0xc6e )]
	public class Onion : Food
	{
		[Constructible]
		public Onion() : this( 1 )
		{
		}

		[Constructible]
		public Onion( int amount ) : base( amount, 0xc6d )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Onion( Serial serial ) : base( serial )
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

	[FlippableAttribute( 0xc70, 0xc71 )]
	public class Lettuce : Food
	{
		[Constructible]
		public Lettuce() : this( 1 )
		{
		}

		[Constructible]
		public Lettuce( int amount ) : base( amount, 0xc70 )
		{
			this.Weight = 1.0;
			this.FillFactor = 1;
		}

		public Lettuce( Serial serial ) : base( serial )
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

	[FlippableAttribute( 0xC6A, 0xC6B )]
	public class Pumpkin : Food
	{
		[Constructible]
		public Pumpkin() : this( 1 )
		{
		}

		[Constructible]
		public Pumpkin( int amount ) : base( amount, 0xC6A )
		{
			this.Weight = 1.0;
			this.FillFactor = 8;
		}

		public Pumpkin( Serial serial ) : base( serial )
		{
		}
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( version < 1 )
			{
				if ( FillFactor == 4 )
					FillFactor = 8;

				if ( Weight == 5.0 )
					Weight = 1.0;
			}
		}
	}

	public class SmallPumpkin : Food
	{
		[Constructible]
		public SmallPumpkin() : this( 1 )
		{
		}

		[Constructible]
		public SmallPumpkin( int amount ) : base( amount, 0xC6C )
		{
			this.Weight = 1.0;
			this.FillFactor = 8;
		}

		public SmallPumpkin( Serial serial ) : base( serial )
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