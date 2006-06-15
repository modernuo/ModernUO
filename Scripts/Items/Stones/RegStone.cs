using System;
using Server.Items;

namespace Server.Items
{
	public class RegStone : Item
	{
		public override string DefaultName
		{
			get { return "a reagent stone"; }
		}

		[Constructable]
		public RegStone() : base( 0xED4 )
		{
			Movable = false;
			Hue = 0x2D1;
		}

		public override void OnDoubleClick( Mobile from )
		{
			BagOfReagents regBag = new BagOfReagents( 50 );

			if ( !from.AddToBackpack( regBag ) )
				regBag.Delete();
		}

		public RegStone( Serial serial ) : base( serial )
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