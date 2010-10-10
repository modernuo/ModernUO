using System;
using Server;

namespace Server.Items
{
	public class ArcaneCircleAddon : BaseAddon
	{
		public override BaseAddonDeed Deed{ get{ return new ArcaneCircleDeed(); } }

		[Constructable]
		public ArcaneCircleAddon()
		{
			AddComponent( new AddonComponent( 0x3080 ), -1,  0, 0 );
			AddComponent( new AddonComponent( 0x3082 ),  0, -1, 0 );
			AddComponent( new AddonComponent( 0x3081 ),  1, -1, 0 );
			AddComponent( new AddonComponent( 0x307D ), -1,  1, 0 );
			AddComponent( new AddonComponent( 0x307F ),  0,  0, 0 );
			AddComponent( new AddonComponent( 0x307E ),  1,  0, 0 );
			AddComponent( new AddonComponent( 0x307C ),  0,  1, 0 );
			AddComponent( new AddonComponent( 0x307B ),  1,  1, 0 );
			AddComponent( new AddonComponent( 0x3083 ),  1,  1, 0 );
		}

		public ArcaneCircleAddon( Serial serial ) : base( serial )
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

	public class ArcaneCircleDeed : BaseAddonDeed
	{
		public override BaseAddon Addon{ get{ return new ArcaneCircleAddon(); } }
		public override int LabelNumber{ get{ return 1072703; } } // arcane circle

		[Constructable]
		public ArcaneCircleDeed()
		{
		}

		public ArcaneCircleDeed( Serial serial ) : base( serial )
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