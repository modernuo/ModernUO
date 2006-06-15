using System;

namespace Server.Items
{
	public abstract class BasePants : BaseClothing
	{
		public BasePants( int itemID ) : this( itemID, 0 )
		{
		}

		public BasePants( int itemID, int hue ) : base( itemID, Layer.Pants, hue )
		{
		}

		public BasePants( Serial serial ) : base( serial )
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

	[FlipableAttribute( 0x152e, 0x152f )]
	public class ShortPants : BasePants
	{
		[Constructable]
		public ShortPants() : this( 0 )
		{
		}

		[Constructable]
		public ShortPants( int hue ) : base( 0x152E, hue )
		{
			Weight = 2.0;
		}

		public ShortPants( Serial serial ) : base( serial )
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

	[FlipableAttribute( 0x1539, 0x153a )]
	public class LongPants : BasePants
	{
		[Constructable]
		public LongPants() : this( 0 )
		{
		}

		[Constructable]
		public LongPants( int hue ) : base( 0x1539, hue )
		{
			Weight = 2.0;
		}

		public LongPants( Serial serial ) : base( serial )
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

	[Flipable( 0x279B, 0x27E6 )]
	public class TattsukeHakama : BasePants
	{
		[Constructable]
		public TattsukeHakama() : this( 0 )
		{
		}

		[Constructable]
		public TattsukeHakama( int hue ) : base( 0x279B, hue )
		{
			Weight = 2.0;
		}

		public TattsukeHakama( Serial serial ) : base( serial )
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

	[FlipableAttribute( 0x2FC3, 0x3179 )]
	public class ElvenPants : BasePants
	{
		public override Race RequiredRace { get { return Race.Elf; } }

		[Constructable]
		public ElvenPants() : this( 0 )
		{
		}

		[Constructable]
		public ElvenPants( int hue ) : base( 0x2FC3, hue )
		{
			Weight = 2.0;
		}

		public ElvenPants( Serial serial ) : base( serial )
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