using System;
using System.Collections.Generic;
using Server;

namespace Server.Mobiles
{
	public class Miller : BaseVendor
	{
		private List<SBInfo> m_SBInfos = new List<SBInfo>();
		protected override List<SBInfo> SBInfos{ get { return m_SBInfos; } }

		[Constructable]
		public Miller() : base( "the miller" )
		{
	
		}

		public override void InitSBInfo()
		{
			m_SBInfos.Add( new SBMiller() );
		}

		public Miller( Serial serial ) : base( serial )
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