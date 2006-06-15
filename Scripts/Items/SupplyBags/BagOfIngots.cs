using System; 
using Server; 
using Server.Items;

namespace Server.Items 
{ 
	public class BagOfingots : Bag 
	{ 
		[Constructable] 
		public BagOfingots() : this( 5000 ) 
		{ 
		} 

		[Constructable] 
		public BagOfingots( int amount ) 
		{ 
			DropItem( new DullCopperIngot   ( amount ) ); 
			DropItem( new ShadowIronIngot   ( amount ) ); 
			DropItem( new CopperIngot   ( amount ) ); 
			DropItem( new BronzeIngot   ( amount ) ); 
			DropItem( new GoldIngot   ( amount ) ); 
			DropItem( new AgapiteIngot   ( amount ) ); 
			DropItem( new VeriteIngot   ( amount ) ); 
			DropItem( new ValoriteIngot   ( amount ) ); 
			DropItem( new IronIngot   ( amount ) );
			DropItem( new Tongs() );
			DropItem( new TinkerTools() );

		} 

		public BagOfingots( Serial serial ) : base( serial ) 
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
