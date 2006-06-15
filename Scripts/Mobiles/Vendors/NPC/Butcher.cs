using System; 
using System.Collections; 
using Server; 

namespace Server.Mobiles 
{ 
	public class Butcher : BaseVendor 
	{ 
		private ArrayList m_SBInfos = new ArrayList(); 
		protected override ArrayList SBInfos{ get { return m_SBInfos; } } 

		[Constructable]
		public Butcher() : base( "the butcher" ) 
		{ 
			SetSkill( SkillName.Anatomy, 45.0, 68.0 );
		} 

		public override void InitSBInfo() 
		{ 
			m_SBInfos.Add( new SBButcher() ); 
		}

		public override void InitOutfit()
		{
			base.InitOutfit();

			AddItem( new Server.Items.HalfApron() );
			AddItem( new Server.Items.Cleaver() );
		}

		public Butcher( Serial serial ) : base( serial ) 
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