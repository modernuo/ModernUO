using System;
using Server;

namespace Server.Items
{
	public class AlchemistsBandage : Item
	{
		public override int LabelNumber{ get{ return 1075452; } } // Alchemist's Bandage

		public override bool Nontransferable { get { return true; } }

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructable]
		public AlchemistsBandage() : base( 0xE21 )
		{
			LootType = LootType.Blessed;
			Hue = 0x482;
		}

		public AlchemistsBandage( Serial serial ) : base( serial )
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
