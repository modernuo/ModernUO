using System;

namespace Server.Mobiles
{
	public class ProximitySpawner : Spawner
	{
		private int m_TriggerRange;
		private TextDefinition m_SpawnMessage;
		private bool m_InstantFlag;

		[CommandProperty( AccessLevel.Developer )]
		public int TriggerRange
		{
			get { return m_TriggerRange; }
			set { m_TriggerRange = value; }
		}

		[CommandProperty( AccessLevel.Developer )]
		public TextDefinition SpawnMessage
		{
			get { return m_SpawnMessage; }
			set { m_SpawnMessage = value; }
		}

		[CommandProperty( AccessLevel.Developer )]
		public bool InstantFlag
		{
			get { return m_InstantFlag; }
			set { m_InstantFlag = value; }
		}

		[Constructible]
		public ProximitySpawner()
		{
		}

		[Constructible]
		public ProximitySpawner( string spawnName )
			: base( spawnName )
		{
		}

		[Constructible]
		public ProximitySpawner( int amount, int minDelay, int maxDelay, int team, int homeRange, string spawnName )
			: base( amount, minDelay, maxDelay, team, homeRange, spawnName )
		{
		}

		[Constructible]
		public ProximitySpawner( int amount, int minDelay, int maxDelay, int team, int homeRange, int triggerRange, string spawnMessage, bool instantFlag, string spawnName )
			: base( amount, minDelay, maxDelay, team, homeRange, spawnName )
		{
			m_TriggerRange = triggerRange;
			m_SpawnMessage = TextDefinition.Parse( spawnMessage );
			m_InstantFlag = instantFlag;
		}

		public ProximitySpawner( int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, params string[] spawnedNames )
			: base( amount, minDelay, maxDelay, team, homeRange, spawnedNames )
		{
		}

		public ProximitySpawner( int amount, TimeSpan minDelay, TimeSpan maxDelay, int team, int homeRange, int triggerRange, TextDefinition spawnMessage, bool instantFlag, params string[] spawnedNames )
			: base( amount, minDelay, maxDelay, team, homeRange, spawnedNames )
		{
			m_TriggerRange = triggerRange;
			m_SpawnMessage = spawnMessage;
			m_InstantFlag = instantFlag;
		}

		public override string DefaultName
		{
			get { return "Proximity Spawner"; }
		}

		public override void DoTimer( TimeSpan delay )
		{
			if ( !Running )
				return;

			End = DateTime.UtcNow + delay;
		}

		public override void Respawn()
		{
			RemoveSpawns();

			End = DateTime.UtcNow;
		}

		public override bool HandlesOnMovement => true;

		public virtual bool ValidTrigger( Mobile m )
		{
			if (m is BaseCreature bc && (bc.IsDeadBondedPet || !(bc.Controlled || bc.Summoned)))
				return false;

			return m.AccessLevel == AccessLevel.Player && ( m.Player || ( m.Alive && !m.Hidden && m.CanBeDamaged() ) );
		}

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			if ( !Running )
				return;

			if ( IsEmpty && End <= DateTime.UtcNow && m.InRange( GetWorldLocation(), m_TriggerRange ) && m.Location != oldLocation && ValidTrigger( m ) )
			{
				TextDefinition.SendMessageTo( m, m_SpawnMessage );

				DoTimer();
				Spawn();

				if ( m_InstantFlag )
				{
					foreach ( ISpawnable spawned in Spawned.Keys )
						if ( spawned is Mobile mobile )
							mobile.Combatant = m;
				}
			}
		}

		public ProximitySpawner( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version

			writer.Write( m_TriggerRange );
			TextDefinition.Serialize( writer, m_SpawnMessage );
			writer.Write( m_InstantFlag );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();

			m_TriggerRange = reader.ReadInt();
			m_SpawnMessage = TextDefinition.Deserialize( reader );
			m_InstantFlag = reader.ReadBool();
		}
	}
}
