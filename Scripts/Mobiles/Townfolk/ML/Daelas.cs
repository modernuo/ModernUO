using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	public class Daelas : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }
		
		public override bool CanTeach{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }
		
		public override void InitSBInfo()
		{		
		}
		
		[Constructable]
		public Daelas() : base( "the aborist" )
		{			
			Name = "Daelas";
		}
		
		public Daelas( Serial serial ) : base( serial )
		{
		}
		
		public override void InitBody()
		{
			InitStats( 100, 100, 25 );
			
			Female = false;
			Race = Race.Elf;
			
			Hue = 0x84DE;
			HairItemID = 0x2FCF;
			HairHue = 0x8F;			
		}
		
		public override void InitOutfit()
		{
			AddItem( new ElvenBoots( 0x901 ) );
			AddItem( new ElvenPants( 0x8AB ) );
			
			Item item;
			
			item = new LeafGloves();
			item.Hue = 0x1BB;
			AddItem( item );			
			
			item = new LeafChest();
			item.Hue = 0x8B0;
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
