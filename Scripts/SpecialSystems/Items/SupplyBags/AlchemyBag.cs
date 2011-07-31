using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class AlchemyBag : Bag
	{
		public override string DefaultName
		{
			get { return "an Alchemy Kit"; }
		}

		[Constructable]
		public AlchemyBag() : this( 1 )
		{
			Movable = true;
			Hue = 0x250;
		}

		[Constructable]
		public AlchemyBag( int amount )
		{
			DropItem( new MortarPestle( 5 ) );
			DropItem( new BagOfReagents( 5000 ) );
			DropItem( new Bottle( 5000 ) );
		}

		public AlchemyBag( Serial serial ) : base( serial )
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