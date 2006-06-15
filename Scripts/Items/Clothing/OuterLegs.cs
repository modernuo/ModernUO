using System;

namespace Server.Items
{
	public abstract class BaseOuterLegs : BaseClothing
	{
		public BaseOuterLegs( int itemID ) : this( itemID, 0 )
		{
		}

		public BaseOuterLegs( int itemID, int hue ) : base( itemID, Layer.OuterLegs, hue )
		{
		}

		public BaseOuterLegs( Serial serial ) : base( serial )
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

	[Flipable( 0x230C, 0x230B )]
	public class FurSarong : BaseOuterLegs
	{
		[Constructable]
		public FurSarong() : this( 0 )
		{
		}

		[Constructable]
		public FurSarong( int hue ) : base( 0x230C, hue )
		{
			Weight = 3.0;
		}

		public FurSarong( Serial serial ) : base( serial )
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

			if ( Weight == 4.0 )
				Weight = 3.0;
		}
	}

	[Flipable( 0x1516, 0x1531 )]
	public class Skirt : BaseOuterLegs
	{
		[Constructable]
		public Skirt() : this( 0 )
		{
		}

		[Constructable]
		public Skirt( int hue ) : base( 0x1516, hue )
		{
			Weight = 4.0;
		}

		public Skirt( Serial serial ) : base( serial )
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

	[Flipable( 0x1537, 0x1538 )]
	public class Kilt : BaseOuterLegs
	{
		[Constructable]
		public Kilt() : this( 0 )
		{
		}

		[Constructable]
		public Kilt( int hue ) : base( 0x1537, hue )
		{
			Weight = 2.0;
		}

		public Kilt( Serial serial ) : base( serial )
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

	[Flipable( 0x279A, 0x27E5 )]
	public class Hakama : BaseOuterLegs
	{
		[Constructable]
		public Hakama() : this( 0 )
		{
		}

		[Constructable]
		public Hakama( int hue ) : base( 0x279A, hue )
		{
			Weight = 2.0;
		}

		public Hakama( Serial serial ) : base( serial )
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