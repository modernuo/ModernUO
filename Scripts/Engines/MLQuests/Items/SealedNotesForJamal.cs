using System;
using Server;

namespace Server.Items
{
	public class SealedNotesForJamal : Item
	{
		public override int LabelNumber{ get{ return 1074998; } } // Sealed Notes For Jamal
		public override double DefaultWeight{ get{ return 1.0; } }

		public override bool Nontransferable { get { return true; } }

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructable]
		public SealedNotesForJamal() : base( 0xEF9 )
		{
			LootType = LootType.Blessed;
		}

		public SealedNotesForJamal( Serial serial ) : base( serial )
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
