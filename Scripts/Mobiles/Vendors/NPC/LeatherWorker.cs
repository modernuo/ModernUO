using System; 
using System.Collections.Generic; 
using Server; 

namespace Server.Mobiles 
{ 
	public class LeatherWorker : BaseVendor 
	{ 
		private List<SBInfo> m_SBInfos = new List<SBInfo>(); 
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } } 

		[Constructable]
		public LeatherWorker() : base( "the leather worker" ) 
		{ 
		} 
		public override void InitSBInfo() 
		{ 
			m_SBInfos.Add( new SBLeatherArmor() ); 
			m_SBInfos.Add( new SBStuddedArmor() ); 
			m_SBInfos.Add( new SBLeatherWorker() ); 
		} 
		public LeatherWorker( Serial serial ) : base( serial ) 
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
