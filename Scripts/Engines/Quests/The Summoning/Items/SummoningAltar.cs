using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Doom
{
	public class SummoningAltar : AbbatoirAddon
	{
		private BoneDemon m_Daemon;

		public BoneDemon Daemon
		{
			get{ return m_Daemon; }
			set
			{
				m_Daemon = value;
				CheckDaemon();
			}
		}

		public void CheckDaemon()
		{
			if ( m_Daemon == null || !m_Daemon.Alive )
			{
				m_Daemon = null;
				Hue = 0;
			}
			else
			{
				Hue = 0x66D;
			}
		}

		[Constructable]
		public SummoningAltar()
		{
		}

		public SummoningAltar( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (Mobile) m_Daemon );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Daemon = reader.ReadMobile() as BoneDemon;

			CheckDaemon();
		}
	}
}