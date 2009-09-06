using System;
using System.Collections;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
	public class PuzzleBox : Item
	{
		private bool m_CanSummon;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool CanSummon { get { return m_CanSummon; } set { m_CanSummon = value; } }

		private WandererOfTheVoid m_Wanderer;

		[CommandProperty( AccessLevel.GameMaster )]
		public WandererOfTheVoid Wanderer { get { return m_Wanderer; } set { m_Wanderer = value; } }

		public override bool ForceShowProperties { get { return true; } }

		[Constructable]
		public PuzzleBox() : base( 0xE80 )
		{
			Movable = false;

			m_Wanderer = null;
		}

		public PuzzleBox( Serial serial ) : base( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.InRange( this.GetWorldLocation(), 3 ) )
				return;

			if ( m_CanSummon && ( m_Wanderer == null || !m_Wanderer.Alive ) )
			{
				m_Wanderer = new WandererOfTheVoid();
				m_Wanderer.MoveToWorld( new Point3D( 467, 94, -1 ), Map.Malas );

				// I am the guardian of the Tomb of Sektu. Suffer my wrath!
				m_Wanderer.PublicOverheadMessage( Network.MessageType.Regular, 0x3B2, 1060002, "" );

				Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), new TimerCallback( SayFakeMessage ) );

				m_CanSummon = false;
			}
		}

		public void SayFakeMessage()
		{
			// You try to pry the box open, when you notice that there is no opening.  It's a fake box.
			PublicOverheadMessage( Network.MessageType.Regular, 0x3B2, 1060003, "" );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_Wanderer );
			writer.Write( (bool) m_CanSummon );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Wanderer = reader.ReadMobile() as WandererOfTheVoid;
			m_CanSummon = reader.ReadBool();
		}
	}
}
