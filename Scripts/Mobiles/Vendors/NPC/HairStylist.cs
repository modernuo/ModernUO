using System; 
using System.Collections; 
using Server; 

namespace Server.Mobiles 
{ 
	public class HairStylist : BaseVendor 
	{ 
		private ArrayList m_SBInfos = new ArrayList(); 
		protected override ArrayList SBInfos{ get { return m_SBInfos; } } 

		[Constructable]
		public HairStylist() : base( "the hair stylist" ) 
		{ 
			SetSkill( SkillName.Alchemy, 80.0, 100.0 );
			SetSkill( SkillName.Magery, 90.0, 110.0 );
			SetSkill( SkillName.TasteID, 85.0, 100.0 );
		} 

		public override void InitSBInfo() 
		{ 
			m_SBInfos.Add( new SBHairStylist() ); 
		} 

		public HairStylist( Serial serial ) : base( serial ) 
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