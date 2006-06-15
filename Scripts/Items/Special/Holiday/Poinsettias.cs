using System;
using Server.Items;
using Server.Network;

namespace Server.Items
{
	public class RedPoinsettia : Item
	{
		[Constructable]
		public RedPoinsettia() : base( 0x2330 )
		{
			Weight = 1.0;
			LootType = LootType.Blessed;
		}

		public RedPoinsettia( Serial serial ) : base( serial )
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

	public class WhitePoinsettia : Item
	{
		[Constructable]
		public WhitePoinsettia() : base( 0x2331 )
		{
			Weight = 1.0;
			LootType = LootType.Blessed;
		}

		public WhitePoinsettia( Serial serial ) : base( serial )
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