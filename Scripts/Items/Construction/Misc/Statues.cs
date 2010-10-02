using System; 

namespace Server.Items 
{ 
	public class StatueSouth : BaseCraftableItem
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable] 
		public StatueSouth() : base(0x139A) 
		{ 
			Weight = 10; 
		} 

		public StatueSouth(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		} 
	} 

	public class StatueSouth2 : BaseCraftableItem
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable] 
		public StatueSouth2() : base(0x1227) 
		{ 
			Weight = 10; 
		} 

		public StatueSouth2(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		} 
	} 

	public class StatueNorth : BaseCraftableItem
	{ 
		[Constructable] 
		public StatueNorth() : base(0x139B) 
		{ 
			Weight = 10; 
		} 

		public StatueNorth(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		} 
	} 

	public class StatueWest : BaseCraftableItem
	{ 
		[Constructable] 
		public StatueWest() : base(0x1226) 
		{ 
			Weight = 10; 
		} 

		public StatueWest(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		} 
	} 

	public class StatueEast : BaseCraftableItem
	{ 
		[Constructable] 
		public StatueEast() : base(0x139C) 
		{ 
			Weight = 10; 
		} 

		public StatueEast(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion 
		} 
	} 

	public class StatueEast2 : BaseCraftableItem 
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable] 
		public StatueEast2() : base(0x1224) 
		{ 
			Weight = 10; 
		} 

		public StatueEast2(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		} 
	} 

	public class StatueSouthEast : BaseCraftableItem
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable] 
		public StatueSouthEast() : base(0x1225) 
		{ 
			Weight = 10; 
		} 

		public StatueSouthEast(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		} 
	} 

	public class BustSouth : BaseCraftableItem
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable] 
		public BustSouth() : base(0x12CB) 
		{ 
			Weight = 10; 
		} 

		public BustSouth(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		} 
	} 

	public class BustEast : BaseCraftableItem
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable] 
		public BustEast() : base(0x12CA) 
		{ 
			Weight = 10; 
		} 

		public BustEast(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion 
		} 
	} 

	public class StatuePegasus : BaseCraftableItem
	{ 
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable] 
		public StatuePegasus() : base(0x139D) 
		{ 
			Weight = 10; 
		} 

		public StatuePegasus(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		} 
	} 

	public class StatuePegasus2 : BaseCraftableItem
	{ 
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable] 
		public StatuePegasus2() : base(0x1228) 
		{ 
			Weight = 10; 
		} 

		public StatuePegasus2(Serial serial) : base(serial) 
		{ 
		} 

		public override bool ForceShowProperties{ get{ return ObjectPropertyList.Enabled; } } 

		public override void Serialize(GenericWriter writer) 
		{ 
			base.Serialize(writer); 

			writer.Write((int) 0); 
		} 

		public override void Deserialize(GenericReader reader) 
		{ 
			base.Deserialize(reader); 

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		} 
	} 

	public class SmallTowerSculpture : BaseCraftableItem
	{
		public override CraftResource DefaultResource{ get{ return CraftResource.Iron; } }
		
		[Constructable]
		public SmallTowerSculpture() : base(0x241A)
		{
			Weight = 20.0;
		}

		public SmallTowerSculpture(Serial serial) : base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write( (int) 0 );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = ( InheritsItem ? OldVersion : reader.ReadInt() ); //Required for BaseCraftableItem insertion
		}
	}
}