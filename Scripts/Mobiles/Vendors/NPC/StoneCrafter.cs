using System;
using System.Collections;
using Server;

namespace Server.Mobiles
{
	[TypeAlias( "Server.Mobiles.GargoyleStonecrafter" )]
	public class StoneCrafter : BaseVendor
	{
		private ArrayList m_SBInfos = new ArrayList();
		protected override ArrayList SBInfos{ get { return m_SBInfos; } }

		public override NpcGuild NpcGuild{ get{ return NpcGuild.TinkersGuild; } }

		[Constructable]
		public StoneCrafter() : base( "the stone crafter" )
		{
			SetSkill( SkillName.Carpentry, 85.0, 100.0 );
		}

		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBStoneCrafter() );
			m_SBInfos.Add( new SBStavesWeapon() );
			m_SBInfos.Add( new SBCarpenter() );
			m_SBInfos.Add( new SBWoodenShields() );
		}

		public StoneCrafter( Serial serial ) : base( serial )
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

			if ( Title == "the stonecrafter" )
				Title = "the stone crafter";
		}
	}
}