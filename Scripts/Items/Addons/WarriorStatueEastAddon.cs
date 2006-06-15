using System;
using Server;

namespace Server.Items
{
	public class WarriorStatueEastAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new WarriorStatueEastDeed(); } }

		[Constructable]
		public WarriorStatueEastAddon()
		{
			AddComponent( new AddonComponent( 0x2D12 ), 0, 0, 0 );
		}

		public WarriorStatueEastAddon( Serial serial ) : base( serial )
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

	public class WarriorStatueEastDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new WarriorStatueEastAddon(); } }
		public override int LabelNumber{ get{ return 1072888; } } // warrior statue (east)

		[Constructable]
		public WarriorStatueEastDeed()
		{
		}

		public WarriorStatueEastDeed( Serial serial ) : base( serial )
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