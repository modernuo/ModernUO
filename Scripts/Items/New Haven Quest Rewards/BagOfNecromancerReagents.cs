using System; 
using Server; 
using Server.Mobiles;

namespace Server.Items 
{ 
	public class BagOfNecromancerReagents : Bag 
	{ 		
		[Constructable] 
		public BagOfNecromancerReagents() : this( 100 ) 
		{ 
		} 
		
		[Constructable] 
		public BagOfNecromancerReagents( int amount ) : base() 
		{ 
			DropItem( new BatWing    ( amount ) );
			DropItem( new GraveDust  ( amount ) );
			DropItem( new DaemonBlood( amount ) );
			DropItem( new NoxCrystal ( amount ) );
			DropItem( new PigIron    ( amount ) );
		} 

		public BagOfNecromancerReagents( Serial serial ) : base( serial ) 
		{ 
		} 

		public override void Serialize( GenericWriter writer ) 
		{ 
			base.Serialize( writer ); 

			writer.WriteEncodedInt( 0 ); // version
		} 

		public override void Deserialize( GenericReader reader ) 
		{ 
			base.Deserialize( reader ); 

			int version = reader.ReadEncodedInt();
		} 
	} 
} 
