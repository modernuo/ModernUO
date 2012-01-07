using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Jothan : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }

		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }

		public override void InitSBInfo()
		{
		}

		[Constructable]
		public Jothan() : base( "the wise" )
		{
			Name = "Elder Jothan";
		}

		public Jothan( Serial serial ) : base( serial )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Female = false;
			Race = Race.Elf;

			Hue = 0x8579;
			HairItemID = 0x2FC2;
			HairHue = 0x2C2;
		}

		public override void InitOutfit()
		{
			AddItem( new ThighBoots() );
			AddItem( new ElvenPants( 0x57A ) );
			AddItem( new ElvenShirt( 0x711 ) );
			AddItem( new Cloak( 0x21 ) );
			AddItem( new Circlet() );
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
