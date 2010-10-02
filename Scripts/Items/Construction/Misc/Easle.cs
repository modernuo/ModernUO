using System;

namespace Server.Items
{
	[Furniture]
	[Flipable( false, 0xF65, 0xF67, 0xF69 )]
	public class Easle : BaseCraftableItem
	{
		[Constructable]
		public Easle() : base( 0xF65 )
		{
		}
		
		[Constructable]
		public Easle( int itemID ) : base( itemID )
		{
			Weight = 24.0;
		}

		public Easle(Serial serial) : base(serial)
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

			if ( Weight == 10.0 || Weight == 25.0 )
				Weight = 24.0;
		}
	}
	
	[Furniture]
	[Flipable( false, 0xF66, 0xF68, 0xF6A )]
	public class EasleWithCanvas : BaseCraftableItem
	{
		[Constructable]
		public EasleWithCanvas() : base( 0xF66 )
		{
		}
		
		[Constructable]
		public EasleWithCanvas( int itemID ) : base( itemID )
		{
			Weight = 24.0;
		}

		public EasleWithCanvas(Serial serial) : base(serial)
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

			int version = reader.ReadInt();
		}
	}
}