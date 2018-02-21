using System;
using Server;

namespace Server.Items
{
	public class SmallStretchedHideEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new SmallStretchedHideEastDeed(); } }

		[Constructible]
		public SmallStretchedHideEastAddon()
		{
			AddComponent( new AddonComponent( 0x1069 ), 0, 0, 0 );
		}

		public SmallStretchedHideEastAddon( Serial serial ) : base( serial )
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

	public class SmallStretchedHideEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new SmallStretchedHideEastAddon(); } }
		public override int LabelNumber{ get{ return 1049401; } } // a small stretched hide deed facing east

		[Constructible]
		public SmallStretchedHideEastDeed()
		{
		}

		public SmallStretchedHideEastDeed( Serial serial ) : base( serial )
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

	public class SmallStretchedHideSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new SmallStretchedHideSouthDeed(); } }

		[Constructible]
		public SmallStretchedHideSouthAddon()
		{
			AddComponent( new AddonComponent( 0x107A ), 0, 0, 0 );
		}

		public SmallStretchedHideSouthAddon( Serial serial ) : base( serial )
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

	public class SmallStretchedHideSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new SmallStretchedHideSouthAddon(); } }
		public override int LabelNumber{ get{ return 1049402; } } // a small stretched hide deed facing south

		[Constructible]
		public SmallStretchedHideSouthDeed()
		{
		}

		public SmallStretchedHideSouthDeed( Serial serial ) : base( serial )
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

	public class MediumStretchedHideEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new MediumStretchedHideEastDeed(); } }

		[Constructible]
		public MediumStretchedHideEastAddon()
		{
			AddComponent( new AddonComponent( 0x106B ), 0, 0, 0 );
		}

		public MediumStretchedHideEastAddon( Serial serial ) : base( serial )
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

	public class MediumStretchedHideEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new MediumStretchedHideEastAddon(); } }
		public override int LabelNumber{ get{ return 1049403; } } // a medium stretched hide deed facing east

		[Constructible]
		public MediumStretchedHideEastDeed()
		{
		}

		public MediumStretchedHideEastDeed( Serial serial ) : base( serial )
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

	public class MediumStretchedHideSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new MediumStretchedHideSouthDeed(); } }

		[Constructible]
		public MediumStretchedHideSouthAddon()
		{
			AddComponent( new AddonComponent( 0x107C ), 0, 0, 0 );
		}

		public MediumStretchedHideSouthAddon( Serial serial ) : base( serial )
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

	public class MediumStretchedHideSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new MediumStretchedHideSouthAddon(); } }
		public override int LabelNumber{ get{ return 1049404; } } // a medium stretched hide deed facing south

		[Constructible]
		public MediumStretchedHideSouthDeed()
		{
		}

		public MediumStretchedHideSouthDeed( Serial serial ) : base( serial )
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