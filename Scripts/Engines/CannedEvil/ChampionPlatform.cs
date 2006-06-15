using System;
using System.Collections;
using Server;
using Server.Items;

namespace Server.Engines.CannedEvil
{
	public class ChampionPlatform : BaseAddon
	{
		private ChampionSpawn m_Spawn;

		public ChampionPlatform( ChampionSpawn spawn )
		{
			m_Spawn = spawn;

			for ( int x = -2; x <= 2; ++x )
				for ( int y = -2; y <= 2; ++y )
					AddComponent( 0x750, x, y, -5 );

			for ( int x = -1; x <= 1; ++x )
				for ( int y = -1; y <= 1; ++y )
					AddComponent( 0x750, x, y, 0 );

			for ( int i = -1; i <= 1; ++i )
			{
				AddComponent( 0x751, i, 2, 0 );
				AddComponent( 0x752, 2, i, 0 );

				AddComponent( 0x753, i, -2, 0 );
				AddComponent( 0x754, -2, i, 0 );
			}

			AddComponent( 0x759, -2, -2, 0 );
			AddComponent( 0x75A, 2, 2, 0 );
			AddComponent( 0x75B, -2, 2, 0 );
			AddComponent( 0x75C, 2, -2, 0 );
		}

		public void AddComponent( int id, int x, int y, int z )
		{
			AddonComponent ac = new AddonComponent( id );

			ac.Hue = 0x497;

			AddComponent( ac, x, y, z );
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if ( m_Spawn != null )
				m_Spawn.Delete();
		}

		public ChampionPlatform( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_Spawn );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Spawn = reader.ReadItem() as ChampionSpawn;

					if ( m_Spawn == null )
						Delete();

					break;
				}
			}
		}
	}
}