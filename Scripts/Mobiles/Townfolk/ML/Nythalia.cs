using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Nythalia : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }
		
		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }
		
		public override void InitSBInfo()
		{		
		}
		
		[Constructable]
		public Nythalia() : base( "the student" )
		{			
			Name = "Nythalia";
		}
		
		public Nythalia( Serial serial ) : base( serial )
		{
		}
		
		public override void InitBody()
		{
			InitStats( 100, 100, 25 );
			
			Female = true;
			Race = Race.Elf;
			
			Hue = 0x840C;
			HairItemID = 0x2045;
			HairHue = 0x453;			
		}
		
		public override void InitOutfit()
		{
			AddItem( new Backpack() );
			AddItem( new Sandals( 0x74A ) );
			AddItem( new Robe( 0x498 ) );
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
