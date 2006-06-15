using System;
using System.Collections;
using Server;
using Server.Items;
using Server.Gumps;
using Server.Prompts;
using Server.Network;
using Server.ContextMenus;

namespace Server.Mobiles
{
	public class Barkeeper : BaseVendor
	{
		private ArrayList m_SBInfos = new ArrayList(); 
		protected override ArrayList SBInfos{ get { return m_SBInfos; } } 

		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBBarkeeper() ); 
		}

		public override VendorShoeType ShoeType{ get{ return Utility.RandomBool() ? VendorShoeType.ThighBoots : VendorShoeType.Boots; } }

		public override void InitOutfit()
		{
			base.InitOutfit();

			AddItem( new HalfApron( RandomBrightHue() ) );
		}

		[Constructable]
		public Barkeeper() : base( "the barkeeper" )
		{
		}

		public Barkeeper( Serial serial ) : base( serial )
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