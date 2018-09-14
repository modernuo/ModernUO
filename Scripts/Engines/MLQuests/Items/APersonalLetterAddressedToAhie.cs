using System;

namespace Server.Items
{
	public class APersonalLetterAddressedToAhie : TransientItem
	{
		public override int LabelNumber => 1073128; // A personal letter addressed to: Ahie

		public override bool Nontransferable => true;

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructible]
		public APersonalLetterAddressedToAhie() : base( 0x14ED, TimeSpan.FromMinutes( 30 ) )
		{
			LootType = LootType.Blessed;
		}

		public APersonalLetterAddressedToAhie( Serial serial ) : base( serial )
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
