using System;
using Server;

namespace Server.Items
{
	public class CrateForSledge : TransientItem
	{
		public override int LabelNumber{ get{ return 1074520; } } // Crate for Sledge

		public override bool Nontransferable { get { return true; } }

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructable]
		public CrateForSledge() : base( 0x1FFF, TimeSpan.FromHours( 1 ) )
		{
			LootType = LootType.Blessed;
		}

		public CrateForSledge( Serial serial ) : base( serial )
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
