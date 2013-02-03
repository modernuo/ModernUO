using System;
using Server;

namespace Server.Items
{
	public class SignedTuitionReimbursementForm : Item
	{
		public override int LabelNumber{ get{ return 1074614; } } // Signed Tuition Reimbursement Form

		public override bool Nontransferable { get { return true; } }

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructable]
		public SignedTuitionReimbursementForm() : base( 0x14F0 )
		{
			LootType = LootType.Blessed;
		}

		public SignedTuitionReimbursementForm( Serial serial ) : base( serial )
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
