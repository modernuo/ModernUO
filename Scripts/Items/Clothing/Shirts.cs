namespace Server.Items
{
	public abstract class BaseShirt : BaseClothing
	{
		public BaseShirt( int itemID ) : this( itemID, 0 )
		{
		}

		public BaseShirt( int itemID, int hue ) : base( itemID, Layer.Shirt, hue )
		{
		}

		public BaseShirt( Serial serial ) : base( serial )
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

	[FlippableAttribute( 0x1efd, 0x1efe )]
	public class FancyShirt : BaseShirt
	{
		[Constructible]
		public FancyShirt() : this( 0 )
		{
		}

		[Constructible]
		public FancyShirt( int hue ) : base( 0x1EFD, hue )
		{
			Weight = 2.0;
		}

		public FancyShirt( Serial serial ) : base( serial )
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

	[FlippableAttribute( 0x1517, 0x1518 )]
	public class Shirt : BaseShirt
	{
		[Constructible]
		public Shirt() : this( 0 )
		{
		}

		[Constructible]
		public Shirt( int hue ) : base( 0x1517, hue )
		{
			Weight = 1.0;
		}

		public Shirt( Serial serial ) : base( serial )
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

			if ( Weight == 2.0 )
				Weight = 1.0;
		}
	}

	[Flippable( 0x2794, 0x27DF )]
	public class ClothNinjaJacket : BaseShirt
	{
		[Constructible]
		public ClothNinjaJacket() : this( 0 )
		{
		}

		[Constructible]
		public ClothNinjaJacket( int hue ) : base( 0x2794, hue )
		{
			Weight = 5.0;
			Layer = Layer.InnerTorso;
		}

		public ClothNinjaJacket( Serial serial ) : base( serial )
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

	public class ElvenShirt : BaseShirt
	{
		public override Race RequiredRace => Race.Elf;

		[Constructible]
		public ElvenShirt() : this( 0 )
		{
		}

		[Constructible]
		public ElvenShirt( int hue ) : base( 0x3175, hue )
		{
			Weight = 2.0;
		}

		public ElvenShirt(Serial serial)
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}

	public class ElvenDarkShirt : BaseShirt
	{
		public override Race RequiredRace => Race.Elf;
		[Constructible]
		public ElvenDarkShirt() : this( 0 )
		{
		}

		[Constructible]
		public ElvenDarkShirt( int hue ) : base( 0x3176, hue )
		{
			Weight = 2.0;
		}

		public ElvenDarkShirt( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}
