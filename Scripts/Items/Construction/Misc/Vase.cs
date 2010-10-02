using System;

namespace Server.Items
{
	public class Vase : BaseCraftableItem
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable]
		public Vase() : base( 0xB46 )
		{
			Weight = 10;
		}

		public Vase( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		}
	}

	public class LargeVase : BaseCraftableItem
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable]
		public LargeVase() : base( 0xB45 )
		{
			Weight = 15;
		}

		public LargeVase( Serial serial ) : base(serial)
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		}
	}

	public class SmallUrn : BaseCraftableItem
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable]
		public SmallUrn() : base( 0x241C )
		{
			Weight = 20.0;
		}

		public SmallUrn(Serial serial) : base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write( (int)0 );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		}
	}
}