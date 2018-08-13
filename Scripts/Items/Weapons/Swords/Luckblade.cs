using System;
using Server.Items;

namespace Server.Items
{
	public class Luckblade : Leafblade
	{
		public override int LabelNumber => 1073522; // luckblade

		[Constructible]
		public Luckblade()
		{
			Attributes.Luck = 20;
		}

		public Luckblade( Serial serial ) : base( serial )
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
