using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Abbein : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }

		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }

		public override void InitSBInfo()
		{
		}

		[Constructable]
		public Abbein() : base( "the wise" )
		{			
			Name = "Elder Abbein";
		}

		public Abbein( Serial serial ) : base( serial )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Female = true;
			Race = Race.Elf;

			Hue = 0x824D;
			HairItemID = 0x2FD1;
			HairHue = 0x321;
		}

		public override void InitOutfit()
		{
			AddItem( new ElvenBoots( 0x74B ) );
			AddItem( new FemaleElvenRobe( 0x8A8 ) );
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