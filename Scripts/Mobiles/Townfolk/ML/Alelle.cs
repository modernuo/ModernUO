using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Alelle : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }

		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }

		public override void InitSBInfo()
		{
		}

		[Constructable]
		public Alelle() : base( "the aborist" )
		{
			Name = "Alelle";
		}

		public Alelle( Serial serial ) : base( serial )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Female = true;
			Race = Race.Elf;

			Hue = 0x8374;
			HairItemID = 0x2FCC;
			HairHue = 0x238;
		}

		public override void InitOutfit()
		{
			AddItem( new ElvenBoots( 0x1BB ) );

			Item item;

			item = new LeafGloves();
			item.Hue = 0x1BB;
			AddItem( item );

			item = new LeafChest();
			item.Hue = 0x37;
			AddItem( item );

			item = new LeafLegs();
			item.Hue = 0x746;
			AddItem( item );
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
