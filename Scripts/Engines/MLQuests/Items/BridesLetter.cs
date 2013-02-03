using System;
using Server;

namespace Server.Items
{
	public class BridesLetter : Item
	{
		public override int LabelNumber{ get{ return 1075301; } } // Bride's Letter

		public override bool Nontransferable { get { return true; } }

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructable]
		public BridesLetter() : base( 0x14ED )
		{
			LootType = LootType.Blessed;
		}

		public BridesLetter( Serial serial ) : base( serial )
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
