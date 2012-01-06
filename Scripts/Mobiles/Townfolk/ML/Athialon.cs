using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Athialon : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }
		
		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }
		
		public override void InitSBInfo()
		{		
		}
		
		[Constructable]
		public Athialon() : base( "the expeditionist" )
		{			
			Name = "Athialon";
		}
		
		public Athialon( Serial serial ) : base( serial )
		{
		}
		
		public override void InitBody()
		{
			InitStats( 100, 100, 25 );
			
			Female = false;
			Race = Race.Elf;
			
			Hue = 0x8382;
			HairItemID = 0x2FC0;
			HairHue = 0x35;			
		}
		
		public override void InitOutfit()
		{
			AddItem( new ElvenBoots( 0x901 ) );
			AddItem( new DiamondMace() );
			AddItem( new WoodlandBelt() );
			
			Item item;
			
			item = new WoodlandLegs();
			item.Hue = 0x3B2;
			AddItem( item );			
			
			item = new WoodlandChest();
			item.Hue = 0x3B2;
			AddItem( item );
			
			item = new WoodlandArms();
			item.Hue = 0x3B2;
			AddItem( item );
			
			item = new WingedHelm();
			item.Hue = 0x3B2;
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
