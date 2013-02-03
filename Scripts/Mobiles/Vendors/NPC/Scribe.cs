using System;
using System.Collections.Generic;
using Server;

namespace Server.Mobiles
{
	public class Scribe : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }

		public override NpcGuild NpcGuild{ get{ return NpcGuild.MagesGuild; } }

		private DateTime m_NextShush;
		public static readonly TimeSpan ShushDelay = TimeSpan.FromMinutes( 1 );

		[Constructable]
		public Scribe() : base( "the scribe" )
		{
			SetSkill( SkillName.EvalInt, 60.0, 83.0 );
			SetSkill( SkillName.Inscribe, 90.0, 100.0 );
		}

		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBScribe() );
		}

		public override VendorShoeType ShoeType
		{
			get{ return Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals; }
		}

		public override void InitOutfit()
		{
			base.InitOutfit();

			AddItem( new Server.Items.Robe( Utility.RandomNeutralHue() ) );
		}

		public override bool HandlesOnSpeech( Mobile from )
		{
			return from.Player;
		}

		public override void OnSpeech( SpeechEventArgs e )
		{
			base.OnSpeech( e );

			if ( !e.Handled && m_NextShush <= DateTime.Now && InLOS( e.Mobile ) )
			{
				Direction = GetDirectionTo( e.Mobile );

				PlaySound( Female ? 0x32F : 0x441 );
				PublicOverheadMessage( Network.MessageType.Regular, 0x3B2, 1073990 ); // Shhhh!

				m_NextShush = DateTime.Now + ShushDelay;
				e.Handled = true;
			}
		}

		public Scribe( Serial serial ) : base( serial )
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