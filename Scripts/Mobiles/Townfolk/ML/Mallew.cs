using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Mallew : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }

		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }

		public override void InitSBInfo()
		{
		}

		[Constructable]
		public Mallew() : base( "the wise" )
		{
			Name = "Elder Mallew";
		}

		public Mallew( Serial serial ) : base( serial )
		{
		}

		public override void InitBody()
		{
			InitStats( 100, 100, 25 );

			Female = false;
			Race = Race.Elf;

			Hue = 0x876C;
			HairItemID = 0x2FD1;
			HairHue = 0x31E;
		}

		public override void InitOutfit()
		{
			AddItem( new ElvenBoots( 0x1BB ) );
			AddItem( new Circlet() );
			AddItem( new Cloak( 0x3B2 ) );

			Item item;

			item = new LeafChest();
			item.Hue = 0x53E;
			AddItem( item );

			item = new LeafArms();
			item.Hue = 0x53E;
			AddItem( item );

			item = new LeafTonlet();
			item.Hue = 0x53E;
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
