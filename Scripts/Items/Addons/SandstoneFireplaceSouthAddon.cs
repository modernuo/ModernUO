namespace Server.Items
{
	public class SandstoneFireplaceSouthAddon : BaseAddon
	{
		public override BaseAddonDeed Deed => new SandstoneFireplaceSouthDeed();

		[Constructible]
		public SandstoneFireplaceSouthAddon()
		{
			AddComponent( new AddonComponent( 0x482 ), -1, 0, 0 );
			AddComponent( new AddonComponent( 0x47B ), 0, 0, 0 );
		}

		public SandstoneFireplaceSouthAddon( Serial serial ) : base( serial )
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

	public class SandstoneFireplaceSouthDeed : BaseAddonDeed
	{
		public override BaseAddon Addon => new SandstoneFireplaceSouthAddon();
		public override int LabelNumber => 1061845; // sandstone fireplace (south)

		[Constructible]
		public SandstoneFireplaceSouthDeed()
		{
		}

		public SandstoneFireplaceSouthDeed( Serial serial ) : base( serial )
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
