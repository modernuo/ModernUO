using System;
using Server;

namespace Server.Items
{
	public class SapOfSosaria : Item
	{
		public override int LabelNumber{ get{ return 1074178; } } // Sap of Sosaria

		[Constructable]
		public SapOfSosaria() : this( 1 )
		{
		}

		[Constructable]
		public SapOfSosaria( int amount ) : base( 0x1848 )
		{
			LootType = LootType.Blessed;
			Stackable = true;
			Amount = amount;
		}

		public SapOfSosaria( Serial serial ) : base( serial )
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
