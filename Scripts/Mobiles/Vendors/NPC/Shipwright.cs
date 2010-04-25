using System; 
using System.Collections.Generic; 
using Server; 

namespace Server.Mobiles 
{ 
	public class Shipwright : BaseVendor 
	{ 
		private List<SBInfo> m_SBInfos = new List<SBInfo>(); 
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } } 

		[Constructable]
		public Shipwright() : base( "the shipwright" ) 
		{ 
			SetSkill( SkillName.Carpentry, 60.0, 83.0 );
			SetSkill( SkillName.Macing, 36.0, 68.0 );
		} 

		public override void InitSBInfo() 
		{ 
			m_SBInfos.Add( new SBShipwright() ); 
		}

		public override void InitOutfit()
		{
			base.InitOutfit();

			AddItem( new Server.Items.SmithHammer() );
		}

		public Shipwright( Serial serial ) : base( serial ) 
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