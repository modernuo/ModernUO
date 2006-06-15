using System;
using Server;
using Server.Mobiles;
using Server.Network;

namespace Server.Factions
{
	public class TownStone : BaseSystemController
	{
		private Town m_Town;

		[CommandProperty( AccessLevel.Counselor, AccessLevel.Administrator )]
		public Town Town
		{
			get{ return m_Town; }
			set
			{
				m_Town = value;

				AssignName( m_Town == null ? null : m_Town.Definition.TownStoneName );
			}
		}

		public override string DefaultName { get { return "faction town stone"; } }

		[Constructable]
		public TownStone() : this( null )
		{
		}

		[Constructable]
		public TownStone( Town town ) : base( 0xEDE )
		{
			Movable = false;
			Town = town;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( m_Town == null )
				return;

			Faction faction = Faction.Find( from );

			if ( faction == null && from.AccessLevel < AccessLevel.GameMaster )
				return; // TODO: Message?

			if ( m_Town.Owner == null || ( from.AccessLevel < AccessLevel.GameMaster && faction != m_Town.Owner ) )
				from.SendLocalizedMessage( 1010332 ); // Your faction does not control this town
			else if ( !m_Town.Owner.IsCommander( from ) )
				from.SendLocalizedMessage( 1005242 ); // Only faction Leaders can use townstones
			else if ( FactionGump.Exists( from ) )
				from.SendLocalizedMessage( 1042160 ); // You already have a faction menu open.
			else if ( from is PlayerMobile )
				from.SendGump( new TownStoneGump( (PlayerMobile)from, m_Town.Owner, m_Town ) );
		}

		public TownStone( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			Town.WriteReference( writer, m_Town );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					Town = Town.ReadReference( reader );
					break;
				}
			}
		}
	}
}