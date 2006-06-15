using System;
using Server;

namespace Server.Items
{
	public class LightFlowerTapestryEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new LightFlowerTapestryEastDeed(); } }

		[Constructable]
		public LightFlowerTapestryEastAddon()
		{
			AddComponent( new AddonComponent( 0xFDC ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0xFDB ), 0, 1, 0 );
		}

		public LightFlowerTapestryEastAddon( Serial serial ) : base( serial )
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

	public class LightFlowerTapestryEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new LightFlowerTapestryEastAddon(); } }
		public override int LabelNumber{ get{ return 1049393; } } // a flower tapestry deed facing east

		[Constructable]
		public LightFlowerTapestryEastDeed()
		{
		}

		public LightFlowerTapestryEastDeed( Serial serial ) : base( serial )
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

	public class LightFlowerTapestrySouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new LightFlowerTapestrySouthDeed(); } }

		[Constructable]
		public LightFlowerTapestrySouthAddon()
		{
			AddComponent( new AddonComponent( 0xFD9 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0xFDA ), 1, 0, 0 );
		}

		public LightFlowerTapestrySouthAddon( Serial serial ) : base( serial )
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

	public class LightFlowerTapestrySouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new LightFlowerTapestrySouthAddon(); } }
		public override int LabelNumber{ get{ return 1049394; } } // a flower tapestry deed facing south

		[Constructable]
		public LightFlowerTapestrySouthDeed()
		{
		}

		public LightFlowerTapestrySouthDeed( Serial serial ) : base( serial )
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

	public class DarkFlowerTapestryEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new DarkFlowerTapestryEastDeed(); } }

		[Constructable]
		public DarkFlowerTapestryEastAddon()
		{
			AddComponent( new AddonComponent( 0xFE0 ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0xFDF ), 0, 1, 0 );
		}

		public DarkFlowerTapestryEastAddon( Serial serial ) : base( serial )
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

	public class DarkFlowerTapestryEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new DarkFlowerTapestryEastAddon(); } }
		public override int LabelNumber{ get{ return 1049395; } } // a dark flower tapestry deed facing east

		[Constructable]
		public DarkFlowerTapestryEastDeed()
		{
		}

		public DarkFlowerTapestryEastDeed( Serial serial ) : base( serial )
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

	public class DarkFlowerTapestrySouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new DarkFlowerTapestrySouthDeed(); } }

		[Constructable]
		public DarkFlowerTapestrySouthAddon()
		{
			AddComponent( new AddonComponent( 0xFDD ), 0, 0, 0 );
			AddComponent( new AddonComponent( 0xFDE ), 1, 0, 0 );
		}

		public DarkFlowerTapestrySouthAddon( Serial serial ) : base( serial )
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

	public class DarkFlowerTapestrySouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new DarkFlowerTapestrySouthAddon(); } }
		public override int LabelNumber{ get{ return 1049396; } } // a dark flower tapestry deed facing south

		[Constructable]
		public DarkFlowerTapestrySouthDeed()
		{
		}

		public DarkFlowerTapestrySouthDeed( Serial serial ) : base( serial )
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