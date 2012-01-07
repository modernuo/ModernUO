using System;
using Server;

namespace Server.Items
{
	public class MinotaurArtifact : Item
	{
		public override int LabelNumber{ get{ return 1074826; } } // Minotaur Artifact
		public override double DefaultWeight{ get{ return 5.0; } }

		[Constructable]
		public MinotaurArtifact() : base( Utility.RandomList( 0xB46, 0xB48, 0x9ED ) )
		{
			if ( ItemID == 0x9ED )
				Weight = 30;

			LootType = LootType.Blessed;
			Hue = 0x100;
		}

		public MinotaurArtifact( Serial serial ) : base( serial )
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
