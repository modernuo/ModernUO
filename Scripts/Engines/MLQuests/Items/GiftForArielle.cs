using System;
using Server;

namespace Server.Items
{
	public class GiftForArielle : BaseContainer
	{
		public override int LabelNumber{ get{ return 1074356; } } // gift for arielle
		public override int DefaultGumpID { get { return 0x41; } }

		public override bool Nontransferable { get { return true; } }

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );
			AddQuestItemProperty( list );
		}

		[Constructable]
		public GiftForArielle() : base( 0x1882 )
		{
			Hue = 0x2C4;
		}

		public GiftForArielle( Serial serial ) : base( serial )
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
