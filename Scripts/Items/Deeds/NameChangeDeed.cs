using System;
using Server.Network;
using Server.Prompts;
using Server.Items;

namespace Server.Items
{
	public class NameChangeDeed : Item
	{
		public override string DefaultName
		{
			get { return "a name change deed"; }
		}

		[Constructable]
		public NameChangeDeed() : base( 0x14F0 )
		{
			base.Weight = 1.0;
		}

		public NameChangeDeed( Serial serial ) : base( serial )
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

		public override void OnDoubleClick( Mobile from )
		{
			// Do namechange
		}
	}
}


