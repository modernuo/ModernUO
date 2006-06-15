using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Network;

namespace Server.Engines.Quests.Ninja
{
	public class JedahEntille : BaseQuester
	{
		[Constructable]
		public JedahEntille() : base( "the Silent" )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Hue = 0x83FE;
			Female = true;
			Body = 0x191;
			Name = "Jedah Entille";
		}

		public override void InitOutfit()
		{
			HairItemID = 0x203C;
			HairHue = 0x6BE;

			AddItem( new PlainDress( 0x528 ) );
			AddItem( new ThighBoots() );
			AddItem( new FloppyHat() );
		}

		public override int TalkNumber{ get{ return -1; } }

		public override void OnTalk( PlayerMobile player, bool contextMenu )
		{
		}

		public JedahEntille( Serial serial ) : base( serial )
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