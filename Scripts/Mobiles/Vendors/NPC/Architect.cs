using System;
using System.Collections;
using Server;

namespace Server.Mobiles
{
	public class Architect : BaseVendor
	{
		private ArrayList m_SBInfos = new ArrayList();
		protected override ArrayList SBInfos{ get { return m_SBInfos; } }

		public override NpcGuild NpcGuild{ get{ return NpcGuild.TinkersGuild; } }

		[Constructable]
		public Architect() : base( "the architect" )
		{
		}

		public override void InitSBInfo()
		{
			if ( !Core.AOS )
				m_SBInfos.Add( new SBHouseDeed() );

			m_SBInfos.Add( new SBArchitect() );
		}

		public Architect( Serial serial ) : base( serial )
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