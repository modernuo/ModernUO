using System;
using Server;

namespace Server.Items
{
	public class ParoxysmusCorrodedStein : Item
	{
		public override int LabelNumber{ get{ return 1072083; } } // Paroxysmus' Corroded Stein

		[Constructable]
		public ParoxysmusCorrodedStein() : base( 0x9D6 )
		{
		}

		public ParoxysmusCorrodedStein( Serial serial ) : base( serial )
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

