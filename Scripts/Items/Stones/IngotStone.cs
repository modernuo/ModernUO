using System;
using Server.Items;

namespace Server.Items
{
	public class IngotStone : Item
	{
		public override string DefaultName
		{
			get { return "an Ingot stone"; }
		}

		[Constructable]
		public IngotStone() : base( 0xED4 )
		{
			Movable = false;
			Hue = 0x480;
		}

		public override void OnDoubleClick( Mobile from )
		{
			BagOfingots ingotBag = new BagOfingots( 5000 );

			if ( !from.AddToBackpack( ingotBag ) )
				ingotBag.Delete();
		}

		public IngotStone( Serial serial ) : base( serial )
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