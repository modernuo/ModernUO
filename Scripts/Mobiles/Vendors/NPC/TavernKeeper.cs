using System.Collections.Generic;

namespace Server.Mobiles 
{ 
	public class TavernKeeper : BaseVendor 
	{ 
		private List<SBInfo> m_SBInfos = new List<SBInfo>(); 
		protected override List<SBInfo> SBInfos => m_SBInfos;

		[Constructible]
		public TavernKeeper() : base( "the tavern keeper" ) 
		{ 
		} 

		public override void InitSBInfo() 
		{ 
			m_SBInfos.Add( new SBTavernKeeper() ); 
		}

		public override void InitOutfit()
		{
			base.InitOutfit();

			AddItem( new Items.HalfApron() );
		}

		public TavernKeeper( Serial serial ) : base( serial ) 
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