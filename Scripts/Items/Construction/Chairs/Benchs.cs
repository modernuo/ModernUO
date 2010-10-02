using System;

namespace Server.Items
{
	[Furniture]
	[Flipable( 0xB2D, 0xB2C )]
	public class WoodenBench : BaseCraftableItem
	{
		[Constructable]
		public WoodenBench() : base( 0xB2C )
		{
			Weight = 1;
		}

		public WoodenBench(Serial serial) : base(serial)
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
			
			if ( Weight == 6 )
				Weight = 1;
		}
	}
}