using System;
using Server;

namespace Server.Items
{
	public class TaintedTreeSample : Item // On OSI the base class is Kindling, and it's ignitable...
	{
		public override int LabelNumber{ get{ return 1074997; } } // Tainted Tree Sample

		public override bool Nontransferable { get { return true; } }

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructable]
		public TaintedTreeSample() : base( 0xDE2 )
		{
			LootType = LootType.Blessed;
			Hue = 0x9D;
		}

		public TaintedTreeSample( Serial serial ) : base( serial )
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
