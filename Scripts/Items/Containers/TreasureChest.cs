namespace Server.Items
{
	[FlippableAttribute( 0xe43, 0xe42 )] 
	public class WoodenTreasureChest : BaseTreasureChest 
	{ 
		[Constructible] 
		public WoodenTreasureChest() : base( 0xE43 ) 
		{ 
		} 

		public WoodenTreasureChest( Serial serial ) : base( serial ) 
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

	[FlippableAttribute( 0xe41, 0xe40 )] 
	public class MetalGoldenTreasureChest : BaseTreasureChest 
	{
		[Constructible] 
		public MetalGoldenTreasureChest() : base( 0xE41 ) 
		{ 
		} 

		public MetalGoldenTreasureChest( Serial serial ) : base( serial ) 
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

	[FlippableAttribute( 0x9ab, 0xe7c )] 
	public class MetalTreasureChest : BaseTreasureChest 
	{
		[Constructible] 
		public MetalTreasureChest() : base( 0x9AB ) 
		{ 
		} 

		public MetalTreasureChest( Serial serial ) : base( serial ) 
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