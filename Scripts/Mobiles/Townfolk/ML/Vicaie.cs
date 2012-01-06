using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Vicaie : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }
		
		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }
		
		public override void InitSBInfo()
		{		
		}
		
		[Constructable]
		public Vicaie() : base( "the wise" )
		{			
			Name = "Elder Vicaie";
		}
		
		public Vicaie( Serial serial ) : base( serial )
		{
		}
		
		public override void InitBody()
		{
			InitStats( 100, 100, 25 );
			
			Female = true;
			Race = Race.Elf;
			
			Hue = 0x8362;
			HairItemID = 0x2FCD;
			HairHue = 0x90;			
		}
		
		public override void InitOutfit()
		{
			AddItem( new ElvenBoots() );
			AddItem( new Tunic( 0x1FA1 ) );
			
			Item item;
			
			item = new LeafLegs();
			item.Hue = 0x3B3;
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
