using System;

namespace Server.Items
{
	[Furniture]
	[Flipable( 0x24D0, 0x24D1, 0x24D2, 0x24D3, 0x24D4 )]
	public class BambooScreen : BaseCraftableItem
	{
		[Constructable]
		public BambooScreen() : base( 0x24D1 )
		{
			Weight = 1.0;
		}

		public BambooScreen(Serial serial) : base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int) 0);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
			
			if ( Weight == 20.0 )
				Weight = 1.0;
		}
	}

	[Furniture]
	[Flipable( 0x24CB, 0x24CC, 0x24CD, 0x24CE, 0x24CF )]
	public class ShojiScreen : BaseCraftableItem
	{
		[Constructable]
		public ShojiScreen() : base( 0x24CB )
		{
			Weight = 1.0;
		}

		public ShojiScreen(Serial serial) : base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int) 0);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
			
			if ( Weight == 20.0 )
				Weight = 1.0;
		}
	}

}