using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Alethanian : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }
		
		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }
		
		public override void InitSBInfo()
		{		
		}
		
		[Constructable]
		public Alethanian() : base( "the wise" )
		{			
			Name = "Elder Alethanian";
		}
		
		public Alethanian( Serial serial ) : base( serial )
		{
		}
		
		public override void InitBody()
		{
			InitStats( 100, 100, 25 );
			
			Female = true;
			Race = Race.Elf;
			
			Hue = 0x876C;
			HairItemID = 0x2FC2;
			HairHue = 0x368;			
		}
		
		public override void InitOutfit()
		{
			AddItem( new ElvenBoots() );
			AddItem( new GemmedCirclet());
			AddItem( new HidePants() );
			AddItem( new HideFemaleChest() );
			AddItem( new HidePauldrons() );
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
