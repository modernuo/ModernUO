using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Network;

namespace Server.Engines.Quests.Ninja
{
	public class HiddenFigure : BaseQuester
	{
		public static int[] Messages = new int[]
			{
				1063191, // They won’t find me here.
				1063192  // Ah, a quiet hideout.
			};

		private int m_Message;

		[CommandProperty( AccessLevel.GameMaster )]
		public int Message{ get{ return m_Message; } set{ m_Message = value; } }

		[Constructable]
		public HiddenFigure()
		{
			m_Message = Utility.RandomList( Messages );
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Hue = Utility.RandomSkinHue();

			Female = Utility.RandomBool();

			if ( Female )
			{
				Body = 0x191;
				Name = NameList.RandomName( "female" );
			}
			else
			{
				Body = 0x190;
				Name = NameList.RandomName( "male" );
			}
		}

		public override void InitOutfit()
		{
			Utility.AssignRandomHair( this );

			AddItem( new TattsukeHakama( GetRandomHue() ) );
			AddItem( new Kasa() );
			AddItem( new HakamaShita( GetRandomHue() ) );

			if ( Utility.RandomBool() )
				AddItem( new Shoes( GetShoeHue() ) );
			else
				AddItem( new Sandals( GetShoeHue() ) );
		}

		public override int GetAutoTalkRange( PlayerMobile pm )
		{
			return 3;
		}

		public override int TalkNumber{ get{ return -1; } }

		public override void OnTalk( PlayerMobile player, bool contextMenu )
		{
			PrivateOverheadMessage( MessageType.Regular, 0x3B2, m_Message, player.NetState );
		}

		public HiddenFigure( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version

			writer.Write( (int) m_Message );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();

			m_Message = reader.ReadInt();
		}
	}
}