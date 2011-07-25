using System;

namespace Server.Items
{
	public class BoneTableAddon : BaseAddon
	{
		public override BaseAddonDeed Deed { get { return new BoneTableDeed(); } }

		[Constructable]
		public BoneTableAddon() : base()
		{
			AddComponent( new LocalizedAddonComponent( 0x2A5C, 1074478 ), 0, 0, 0 );
		}

		public BoneTableAddon( Serial serial ) : base( serial )
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

	public class BoneTableDeed : BaseAddonDeed
	{
		public override BaseAddon Addon { get { return new BoneTableAddon(); } }
		public override int LabelNumber { get { return 1074478; } } // Bone table

		[Constructable]
		public BoneTableDeed() : base()
		{
			LootType = LootType.Blessed;
		}

		public BoneTableDeed( Serial serial ) : base( serial )
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
