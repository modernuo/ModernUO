using System;
using Server;

namespace Server.Items
{
	public class Cauldron : Item
	{
		public override string DefaultName
		{
			get { return "a cauldron"; }
		}

		[Constructable]
		public Cauldron() : base( 0x9ED )
		{
			Weight = 1.0;
		}

		public Cauldron( Serial serial ) : base( serial )
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
	}
}