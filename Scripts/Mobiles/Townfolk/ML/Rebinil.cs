using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Rebinil : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }

		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }

		public override void InitSBInfo()
		{
		}

		[Constructable]
		public Rebinil() : base( "the healer" )
		{
			Name = "Rebinil";
		}

		public Rebinil( Serial serial ) : base( serial )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Female = true;
			Race = Race.Elf;

			Hue = 0x83E7;
			HairItemID = 0x2FD0;
			HairHue = 0x26B;
		}

		public override void InitOutfit()
		{
			AddItem( new Sandals( 0x719 ) );
			AddItem( new FemaleElvenRobe( 0x757 ) );
			AddItem( new RoyalCirclet() );
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