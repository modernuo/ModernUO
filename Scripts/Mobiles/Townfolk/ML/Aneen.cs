using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Aneen : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }

		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }

		public override void InitSBInfo()
		{
		}

		[Constructable]
		public Aneen() : base( "the keeper of tradition" )
		{
			Name = "Lorekeeper Aneen";
		}

		public Aneen( Serial serial ) : base( serial )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Female = false;
			Race = Race.Elf;

			Hue = 0x83E5;
			HairItemID = 0x2FBF;
			HairHue = 0x90;
		}

		public override void InitOutfit()
		{
			AddItem( new Sandals( 0x1BB ) );
			AddItem( new MaleElvenRobe( 0x48F ) );
			AddItem( new Item( 0xDF2 ) );
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
