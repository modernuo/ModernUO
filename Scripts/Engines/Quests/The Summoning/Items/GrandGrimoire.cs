using System;

namespace Server.Engines.Quests.Doom
{
	public class GrandGrimoire : Item
	{
		public override int LabelNumber{ get{ return 1060801; } } // The Grand Grimoire

		[Constructable]
		public GrandGrimoire() : base( 0xEFA )
		{
			Weight = 1.0;
			Hue = 0x835;
			Layer = Layer.OneHanded;
			LootType = LootType.Blessed;
		}

		public GrandGrimoire( Serial serial ) : base( serial )
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