using System;
using Server;

namespace Server.Items
{
	public class UnicornRibs : Item
	{
		public override int LabelNumber{ get{ return 1074611; } } // Unicorn Ribs

		[Constructable]
		public UnicornRibs() : this( 1 )
		{
		}

		[Constructable]
		public UnicornRibs( int amount ) : base( 0x9F1 )
		{
			LootType = LootType.Blessed;
			Hue = 0x14B;
			Stackable = true;
			Amount = amount;
		}

		public UnicornRibs( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // Version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}
