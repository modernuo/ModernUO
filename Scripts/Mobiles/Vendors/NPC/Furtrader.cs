using System;
using System.Collections;
using Server;

namespace Server.Mobiles
{
	public class Furtrader : BaseVendor
	{
		private ArrayList m_SBInfos = new ArrayList();
		protected override ArrayList SBInfos{ get { return m_SBInfos; } }

		[Constructable]
		public Furtrader() : base( "the furtrader" )
		{
			SetSkill( SkillName.Camping, 55.0, 78.0 );
			//SetSkill( SkillName.Alchemy, 60.0, 83.0 );
			SetSkill( SkillName.AnimalLore, 85.0, 100.0 );
			SetSkill( SkillName.Cooking, 45.0, 68.0 );
			SetSkill( SkillName.Tracking, 36.0, 68.0 );
		}

		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBFurtrader() );
		}

		public Furtrader( Serial serial ) : base( serial )
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