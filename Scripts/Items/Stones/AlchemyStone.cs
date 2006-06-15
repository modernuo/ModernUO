using System;
using Server.Items;

namespace Server.Items
{
	public class AlchemyStone : Item
	{
		public override string DefaultName
		{
			get { return "an Alchemist Supply Stone"; }
		}

		[Constructable]
		public AlchemyStone() : base( 0xED4 )
		{
			Movable = false;
			Hue = 0x250;
		}

		public override void OnDoubleClick( Mobile from )
		{
			AlchemyBag alcBag = new AlchemyBag();

			if ( !from.AddToBackpack( alcBag ) )
				alcBag.Delete();
		}

		public AlchemyStone( Serial serial ) : base( serial )
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