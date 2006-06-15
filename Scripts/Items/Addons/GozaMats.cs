using System;
using Server;

namespace Server.Items
{
	public class GozaMatEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new GozaMatEastDeed(); } }

		[Constructable]
		public GozaMatEastAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0x28a4, 1030688  ), 1, 0, 0 );
			AddComponent( new LocalizedAddonComponent( 0x28a5, 1030688 ), 0, 0, 0 );
		}

		public GozaMatEastAddon( Serial serial ) : base( serial )
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

	public class GozaMatEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new GozaMatEastAddon(); } }
		public override int LabelNumber{ get{ return 1030404; } } // goza (east)

		[Constructable]
		public GozaMatEastDeed()
		{
		}

		public GozaMatEastDeed( Serial serial ) : base( serial )
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

	public class GozaMatSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new GozaMatSouthDeed(); } }

		[Constructable]
		public GozaMatSouthAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0x28a6, 1030688 ), 0, 1, 0 );
			AddComponent( new LocalizedAddonComponent( 0x28a7, 1030688 ), 0, 0, 0 );
		}

		public GozaMatSouthAddon( Serial serial ) : base( serial )
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

	public class GozaMatSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new GozaMatSouthAddon(); } }
		public override int LabelNumber{ get{ return 1030405; } } // goza (south)

		[Constructable]
		public GozaMatSouthDeed()
		{
		}

		public GozaMatSouthDeed( Serial serial ) : base( serial )
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

	public class SquareGozaMatEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new SquareGozaMatEastDeed(); } }
		public override int LabelNumber{ get{ return 1030688; } } // goza mat

		[Constructable]
		public SquareGozaMatEastAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0x28a8, 1030688 ), 0, 0, 0 );
		}

		public SquareGozaMatEastAddon( Serial serial ) : base( serial )
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

	public class SquareGozaMatEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new SquareGozaMatEastAddon(); } }
		public override int LabelNumber{ get{ return 1030407; } } // square goza (east)

		[Constructable]
		public SquareGozaMatEastDeed()
		{
		}

		public SquareGozaMatEastDeed( Serial serial ) : base( serial )
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

	public class SquareGozaMatSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new SquareGozaMatSouthDeed(); } }

		[Constructable]
		public SquareGozaMatSouthAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0x28a9, 1030688 ), 0, 0, 0 );
		}

		public SquareGozaMatSouthAddon( Serial serial ) : base( serial )
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

	public class SquareGozaMatSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new SquareGozaMatSouthAddon(); } }
		public override int LabelNumber{ get{ return 1030406; } } // square goza (south)


		[Constructable]
		public SquareGozaMatSouthDeed()
		{
		}

		public SquareGozaMatSouthDeed( Serial serial ) : base( serial )
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

	public class BrocadeGozaMatEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new BrocadeGozaMatEastDeed(); } }

		[Constructable]
		public BrocadeGozaMatEastAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0x28AB, 1030688 ), 0, 0, 0 );
			AddComponent( new LocalizedAddonComponent( 0x28AA, 1030688 ), 1, 0, 0 );
		}

		public BrocadeGozaMatEastAddon( Serial serial ) : base( serial )
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

	public class BrocadeGozaMatEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new BrocadeGozaMatEastAddon(); } }
		public override int LabelNumber{ get{ return 1030408; } } // brocade goza (east)

		[Constructable]
		public BrocadeGozaMatEastDeed()
		{
		}

		public BrocadeGozaMatEastDeed( Serial serial ) : base( serial )
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
	public class BrocadeGozaMatSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new BrocadeGozaMatSouthDeed(); } }

		[Constructable]
		public BrocadeGozaMatSouthAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0x28AD, 1030688 ), 0, 0, 0 );
			AddComponent( new LocalizedAddonComponent( 0x28AC, 1030688 ), 0, 1, 0 );
		}

		public BrocadeGozaMatSouthAddon( Serial serial ) : base( serial )
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

	public class BrocadeGozaMatSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new BrocadeGozaMatSouthAddon(); } }
		public override int LabelNumber{ get{ return 1030409; } } // brocade goza (south)

		[Constructable]
		public BrocadeGozaMatSouthDeed()
		{
		}

		public BrocadeGozaMatSouthDeed( Serial serial ) : base( serial )
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
	public class BrocadeSquareGozaMatEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new BrocadeSquareGozaMatEastDeed(); } }

		[Constructable]
		public BrocadeSquareGozaMatEastAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0x28AE, 1030688 ), 0, 0, 0 );
		}

		public BrocadeSquareGozaMatEastAddon( Serial serial ) : base( serial )
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

	public class BrocadeSquareGozaMatEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new BrocadeSquareGozaMatEastAddon(); } }
		public override int LabelNumber{ get{ return 1030411; } } // brocade square goza (east)

		[Constructable]
		public BrocadeSquareGozaMatEastDeed()
		{
		}

		public BrocadeSquareGozaMatEastDeed( Serial serial ) : base( serial )
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

	public class BrocadeSquareGozaMatSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new BrocadeSquareGozaMatSouthDeed(); } }

		[Constructable]
		public BrocadeSquareGozaMatSouthAddon()
		{
			AddComponent( new LocalizedAddonComponent( 0x28AF, 1030688 ), 0, 0, 0 );
		}

		public BrocadeSquareGozaMatSouthAddon( Serial serial ) : base( serial )
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

	public class BrocadeSquareGozaMatSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new BrocadeSquareGozaMatSouthAddon(); } }
		public override int LabelNumber{ get{ return 1030410; } } // brocade square goza (south)


		[Constructable]
		public BrocadeSquareGozaMatSouthDeed()
		{
		}

		public BrocadeSquareGozaMatSouthDeed( Serial serial ) : base( serial )
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