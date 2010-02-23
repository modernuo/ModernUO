using System;
using System.Collections.Generic;

namespace Server.Misc
{
	public class TreasuresOfTokunoPersistance : Item
	{
		private static TreasuresOfTokunoPersistance m_Instance;

		public static TreasuresOfTokunoPersistance Instance{ get{ return m_Instance; } }

		public override string DefaultName
		{
			get { return "TreasuresOfTokuno Persistance - Internal"; }
		}

		public static void Initialize()
		{
			if ( m_Instance == null )
				new TreasuresOfTokunoPersistance();
		}

		public TreasuresOfTokunoPersistance() : base( 1 )
		{
			Movable = false;

			if ( m_Instance == null || m_Instance.Deleted )
				m_Instance = this;
			else
				base.Delete();
		}

		public TreasuresOfTokunoPersistance( Serial serial ) : base( serial )
		{
			m_Instance = this;
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.WriteEncodedInt( (int)TreasuresOfTokuno.RewardEra );
			writer.WriteEncodedInt( (int)TreasuresOfTokuno.DropEra );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					TreasuresOfTokuno.RewardEra = (TreasuresOfTokunoEra)reader.ReadEncodedInt();
					TreasuresOfTokuno.DropEra = (TreasuresOfTokunoEra)reader.ReadEncodedInt();
					
					break;
				}
			}
		}

		public override void Delete()
		{
		}
	}
}
