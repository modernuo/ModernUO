using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class InsaneDryad : MLDryad
	{
		public override string CorpseName => "an insane dryad corpse";
		public override bool InitialInnocent => false;

		public override string DefaultName => "an insane dryad";

		[Constructible]
		public InsaneDryad()
		{
			// TODO: Perhaps these should have negative karma?
		}

		/*
		// TODO: uncomment once added
		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			if ( Utility.RandomDouble() < 0.1 )
				c.DropItem( new ParrotItem() );
		}
		*/

		public InsaneDryad( Serial serial )
			: base( serial )
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
