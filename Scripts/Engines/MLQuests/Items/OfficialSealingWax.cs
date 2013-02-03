using System;
using Server;

namespace Server.Items
{
	public class OfficialSealingWax : Item
	{
		public override int LabelNumber{ get{ return 1072744; } } // Official Sealing Wax

		public override bool Nontransferable { get { return true; } }

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructable]
		public OfficialSealingWax() : base( 0x1426 )
		{
			LootType = LootType.Blessed;
			Hue = 0x84;
		}

		public OfficialSealingWax( Serial serial ) : base( serial )
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
