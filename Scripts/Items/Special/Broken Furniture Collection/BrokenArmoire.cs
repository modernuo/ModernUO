using System;

namespace Server.Items
{
	[Flipable( 0xC12, 0xC13 )]
	public class BrokenArmoireComponent : AddonComponent
	{
		public override int LabelNumber { get { return 1076262; } } // Broken Armoire

		public BrokenArmoireComponent() : base( 0xC12 )
		{
		}

		public BrokenArmoireComponent( Serial serial ) : base( serial )
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

	public class BrokenArmoireAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new BrokenArmoireDeed(); } }

		[Constructable]
		public BrokenArmoireAddon() : base()
		{
			AddComponent( new BrokenArmoireComponent(), 0, 0, 0 );
		}

		public BrokenArmoireAddon( Serial serial ) : base( serial )
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

	public class BrokenArmoireDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new BrokenArmoireAddon(); } }
		public override int LabelNumber { get { return 1076262; } } // Broken Armoire

		[Constructable]
		public BrokenArmoireDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public BrokenArmoireDeed( Serial serial ) : base( serial )
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
