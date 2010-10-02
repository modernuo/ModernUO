using System;

namespace Server.Items
{
	[Furniture]
	[Flipable( false, 0xEBB, 0xEBC )]
	public class TallMusicStand : BaseCraftableItem
	{
		public override bool DisplaysResource{ get{ return false; } }
		
		[Constructable]
		public TallMusicStand() : this( 0xEBB )
		{
		}
		
		[Constructable]
		public TallMusicStand( int itemID ) : base( itemID )
		{
			Weight = 10.0;
		}

		public TallMusicStand(Serial serial) : base(serial)
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

			if ( Weight == 8.0 )
				Weight = 10.0;
		}
	}

	[Furniture]
	[Flipable( false, 0xEB6,0xEB8 )]
	public class ShortMusicStand : BaseCraftableItem
	{
		public override bool DisplaysResource{ get{ return false; } }
		
		[Constructable]
		public ShortMusicStand() : this( 0xEB6 )
		{
		}
		
		[Constructable]
		public ShortMusicStand( int itemID ) : base( itemID )
		{
			Weight = 10.0;
		}

		public ShortMusicStand(Serial serial) : base(serial)
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
