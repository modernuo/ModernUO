using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Regions;
using Server.Spells.Fourth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Spells.Chivalry;

namespace Server.Misc
{
	public class PoisonRoom
	{
		public virtual int GasEffects { get { return 3; } }
		public virtual double TimePerPoisonCast { get { return 3; } }
		public virtual int TicksPerPoisonLevel { get { return 15; } }
		public virtual Poison MinPoisonLevel { get { return Poison.Lesser; } }
		public virtual Poison MaxPoisonLevel { get { return Poison.Lethal; } }
		public virtual int GasEffectHeight { get { return 0; } }
		public virtual bool HasPoisonSoundEffect { get { return true; } }

		protected PoisonRoomRegion m_Region;
		protected PoisonRoomTimer m_Timer;
		protected int m_EffectHue = 1166;
		protected int m_EffectId = 4518;
		protected int m_EffectDuration = 16;
		protected List<Mobile> m_Dead;

		public List<Mobile> Dead
		{
			get { return m_Dead; }
		}

		public PoisonRoom( string name, Map map, int priority, Rectangle2D[] rect, int LightLevel )
		{
			m_Region = new PoisonRoomRegion( this, name, map, priority, rect, LightLevel );
			m_Region.Register();
			m_Dead = new List<Mobile>();
		}

		public virtual void OnEnter( Mobile m )
		{
			if ( m.AccessLevel == AccessLevel.Player && m_Timer == null )
				StartGas();
		}

		public virtual void OnPoison( Poison poison, int tick )
		{
		}

		public virtual void StartGas()
		{
			m_Timer = new PoisonRoomTimer( this );
			m_Timer.Start();
		}

		public virtual void StopGas()
		{
			if ( m_Timer != null )
				m_Timer.Stop();

			m_Timer = null;
		}

		public virtual void OnExit( Mobile m )
		{
			if ( GetPoisonableMobiles( m ).Count == 0 )
			{
				StopGas();
				return;
			}
		}

		public virtual void OnDeath( Mobile m )
		{
			m_Dead.Add( m );
		}

		public virtual void DoGasEffect()
		{
			for ( int i = 0; i < this.GasEffects; ++i )
				RandomGasEffect();
		}

		public virtual void RandomGasEffect()
		{
			Point3D location = m_Region.RandomSpawnLocation( GasEffectHeight, true, true, Point3D.Zero, 0 );

			if ( location != Point3D.Zero )
			{
				Effects.SendLocationEffect( location, m_Region.Map, 4518, 16, 1, 1166, 0 );

				if ( HasPoisonSoundEffect )
					Effects.PlaySound( location, m_Region.Map, 0x231 );
			}
		}

		public virtual void PoisonPlayers( Poison poison )
		{
			List<Mobile> list = GetPoisonableMobiles( null );

			foreach ( Mobile m in list )
			{
				if ( ( m.Poison == null || m.Poison.Level < poison.Level ) )
					m.ApplyPoison( null, poison );
			}
		}

		public int CurrentPoisonLevel()
		{
			if ( m_Timer != null )
				return m_Timer.CurrentPoisonLevel();

			return 0;
		}

		protected virtual bool IsPoisonable( Mobile m )
		{
			if ( m.AccessLevel == AccessLevel.Player && m.CheckAlive() && ( m.Player || ( m is BaseCreature && ((BaseCreature)m).Controlled ) ) )
				return true;

			return false;
		}

		public virtual List<Mobile> GetPoisonableMobiles( Mobile toexclude )
		{
			List<Mobile> list = new List<Mobile>();
			List<Mobile> templist = m_Region.GetMobiles();

			foreach ( Mobile m in templist )
			{
				if ( IsPoisonable( m ) )
					list.Add( m );
			}

			if ( toexclude != null )
				list.Remove( toexclude );

			return list;
		}

		public class PoisonRoomTimer : Timer
		{
			private PoisonRoom m_Room;
			private int m_Count = 0;

			public PoisonRoomTimer( PoisonRoom room ) : base( TimeSpan.FromSeconds( 0 ), TimeSpan.FromSeconds( room.TimePerPoisonCast ) )
			{
				m_Room = room;
			}

			public int CurrentPoisonLevel()
			{
				int poisonLevel = m_Room.MinPoisonLevel.Level + m_Count / m_Room.TicksPerPoisonLevel;

				if ( poisonLevel > m_Room.MaxPoisonLevel.Level )
					poisonLevel = m_Room.MaxPoisonLevel.Level;

				return poisonLevel;
			}

			protected override void OnTick()
			{
				m_Count++;

				int poisonLevel = CurrentPoisonLevel();

				m_Room.OnPoison( Poison.GetPoison( poisonLevel ), m_Count );
				m_Room.PoisonPlayers( Poison.GetPoison( poisonLevel ) );
				m_Room.DoGasEffect();
			}
		}

		public class PoisonRoomRegion : BaseRegion
		{
			private PoisonRoom m_Room;
			private int m_LightLevel;

			public PoisonRoomRegion( PoisonRoom room, string name, Map map, int priority, Rectangle2D[] rect, int lightlevel ) : base( name, map, priority, rect )
			{
				ExcludeFromParentSpawns = true;
				m_Room = room;
				m_LightLevel = lightlevel;
			}

			public override void OnEnter( Mobile m )
			{
				base.OnEnter( m );

				m_Room.OnEnter( m );
			}

			public override void OnExit( Mobile m )
			{
				base.OnExit( m );

				m_Room.OnExit( m );
			}

			public override void OnDeath( Mobile m )
			{
				base.OnDeath( m );

				m_Room.OnDeath( m );
			}

			public override bool OnResurrect( Mobile from )
			{
				return true;
			}

			public override void AlterLightLevel( Mobile m, ref int global, ref int personal )
			{
				global = m_LightLevel;
			}

			public override bool OnBeginSpellCast( Mobile m, ISpell s )
			{
				if ( ( s is MarkSpell || s is GateTravelSpell || s is RecallSpell || s is SacredJourneySpell ) && m.AccessLevel == AccessLevel.Player )
				{
					m.SendLocalizedMessage( 501802 ); // Thy spell doth not appear to work...
					return false;
				}

				return base.OnBeginSpellCast( m, s );
			}
		}
	}
}
