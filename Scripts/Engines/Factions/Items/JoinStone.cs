using System;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
	public class JoinStone : BaseSystemController
	{
		private Faction m_Faction;

		[CommandProperty( AccessLevel.Counselor, AccessLevel.Administrator )]
		public Faction Faction
		{
			get{ return m_Faction; }
			set
			{
				m_Faction = value;

				Hue = ( m_Faction == null ? 0 : m_Faction.Definition.HueJoin );
				AssignName( m_Faction == null ? null : m_Faction.Definition.SignupName );
			}
		}

		public override string DefaultName { get { return "faction signup stone"; } }

		[Constructable]
		public JoinStone() : this( null )
		{
		}

		[Constructable]
		public JoinStone( Faction faction ) : base( 0xEDC )
		{
			Movable = false;
			Faction = faction;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( m_Faction == null )
				return;

			if ( !from.InRange( GetWorldLocation(), 2 ) )
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
			else if ( FactionGump.Exists( from ) )
				from.SendLocalizedMessage( 1042160 ); // You already have a faction menu open.
			else if ( Faction.Find( from ) == null && from is PlayerMobile )
				from.SendGump( new JoinStoneGump( (PlayerMobile) from, m_Faction ) );
		}

		public JoinStone( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			Faction.WriteReference( writer, m_Faction );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					Faction = Faction.ReadReference( reader );
					break;
				}
			}
		}
	}
}