using System;

namespace Server.Items
{
	[Furniture]
	public class Stool : BaseCraftableItem
	{
		[Constructable]
		public Stool() : base( 0xA2A )
		{
			Weight = 10.0;
		}

		public Stool(Serial serial) : base(serial)
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

			if ( Weight == 6.0 )
				Weight = 10.0;
		}
	}

	[Furniture]
	public class FootStool : BaseCraftableItem
	{
		[Constructable]
		public FootStool() : base( 0xB5E )
		{
			Weight = 10.0;
		}

		public FootStool(Serial serial) : base(serial)
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

			if ( Weight == 6.0 )
				Weight = 10.0;
		}
	}
}