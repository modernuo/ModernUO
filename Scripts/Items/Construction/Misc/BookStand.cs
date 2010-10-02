using System;

namespace Server.Items
{
	[Furniture]
	[Flipable( false, 0x2DDD, 0x2DDE )]
	public class ElvenBookStand : BaseCraftableItem
	{
		[Constructable]
		public ElvenBookStand() : base( 0x2DDD )
		{
			Weight = 20.0;
		}

		public ElvenBookStand(Serial serial) : base(serial)
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

			int version = reader.ReadInt(); //Required for BaseCraftableItem insertion
		}
	}
}
