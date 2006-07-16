using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Factions.AI
{
	public enum ReactionType
	{
		Ignore,
		Warn,
		Attack
	}

	public enum MovementType
	{
		Stand,
		Patrol,
		Follow
	}

	public class Reaction
	{
		private Faction m_Faction;
		private ReactionType m_Type;

		public Faction Faction{ get{ return m_Faction; } }
		public ReactionType Type{ get{ return m_Type; } set{ m_Type = value; } }

		public Reaction( Faction faction, ReactionType type )
		{
			m_Faction = faction;
			m_Type = type;
		}

		public Reaction( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 0:
				{
					m_Faction = Faction.ReadReference( reader );
					m_Type = (ReactionType) reader.ReadEncodedInt();

					break;
				}
			}
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			Faction.WriteReference( writer, m_Faction );
			writer.WriteEncodedInt( (int) m_Type );
		}
	}

	public class Orders
	{
		private BaseFactionGuard m_Guard;

		private List<Reaction> m_Reactions;
		private MovementType m_Movement;
		private Mobile m_Follow;

		public BaseFactionGuard Guard{ get{ return m_Guard; } }

		public MovementType Movement{ get{ return m_Movement; } set{ m_Movement = value; } }
		public Mobile Follow{ get{ return m_Follow; } set{ m_Follow = value; } }

		public Reaction GetReaction( Faction faction )
		{
			Reaction reaction;

			for ( int i = 0; i < m_Reactions.Count; ++i )
			{
				reaction = m_Reactions[i];

				if ( reaction.Faction == faction )
					return reaction;
			}

			reaction = new Reaction( faction, ( faction == null || faction == m_Guard.Faction ) ? ReactionType.Ignore : ReactionType.Attack );
			m_Reactions.Add( reaction );

			return reaction;
		}

		public void SetReaction( Faction faction, ReactionType type )
		{
			Reaction reaction = GetReaction( faction );

			reaction.Type = type;
		}

		public Orders( BaseFactionGuard guard )
		{
			m_Guard = guard;
			m_Reactions = new List<Reaction>();
			m_Movement = MovementType.Patrol;
		}

		public Orders( BaseFactionGuard guard, GenericReader reader )
		{
			m_Guard = guard;

			int version = reader.ReadEncodedInt();

			switch ( version )
			{
				case 1:
				{
					m_Follow = reader.ReadMobile();
					goto case 0;
				}
				case 0:
				{
					int count = reader.ReadEncodedInt();
					m_Reactions = new List<Reaction>( count );

					for ( int i = 0; i < count; ++i )
						m_Reactions.Add( new Reaction( reader ) );

					m_Movement = (MovementType)reader.ReadEncodedInt();

					break;
				}
			}
		}

		public void Serialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 1 ); // version

			writer.Write( (Mobile) m_Follow );

			writer.WriteEncodedInt( (int) m_Reactions.Count );

			for ( int i = 0; i < m_Reactions.Count; ++i )
				m_Reactions[i].Serialize( writer );

			writer.WriteEncodedInt( (int) m_Movement );
		}
	}
}