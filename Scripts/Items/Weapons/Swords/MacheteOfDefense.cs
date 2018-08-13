using System;
using Server.Items;

namespace Server.Items
{
	public class MacheteOfDefense : ElvenMachete
	{
		public override int LabelNumber => 1073535; // machete of defense

		[Constructible]
		public MacheteOfDefense()
		{
			Attributes.DefendChance = 5;
		}

		public MacheteOfDefense( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}
