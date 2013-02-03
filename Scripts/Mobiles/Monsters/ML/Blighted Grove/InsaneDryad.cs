using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "an insane dryad corpse" )]
	public class InsaneDryad : MLDryad
	{
		public override bool InitialInnocent { get { return false; } }

		[Constructable]
		public InsaneDryad()
		{
			Name = "an insane dryad";

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
