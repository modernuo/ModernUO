using System;
using Server;
using Server.Mobiles;
using Server.Spells;
using System.Collections;

namespace Server.Items
{
	public class PoolOfAcid : Item
	{
		private TimeSpan m_Duration;
		private int m_MinDamage;
		private int m_MaxDamage;

		private DateTime m_Created;

		private bool m_Drying;

		private Timer m_Timer;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Drying
		{
			get
			{
				return m_Drying;
			}
			set
			{
				m_Drying = value;

				if( m_Drying )
					ItemID = 0x122A;
				else
					ItemID = 0x122B;
			}
		}


		[CommandProperty( AccessLevel.GameMaster )]
		public TimeSpan Duration{ get{ return m_Duration; } set{ m_Duration = value; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int MinDamage
		{
			get
			{
				return m_MinDamage;
			}
			set
			{
				if ( value < 1 )
					value = 1;

				m_MinDamage = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int MaxDamage
		{
			get
			{
				return m_MaxDamage;
			}
			set
			{
				if ( value < 1 )
					value = 1;

				if ( value < MinDamage )
					value = MinDamage;

				m_MaxDamage = value;
			}
		}

		[Constructable]
		public PoolOfAcid() : this( TimeSpan.FromSeconds( 10.0 ), 2, 5 )
		{
		}

		public override string DefaultName { get { return "a pool of acid"; } }

		[Constructable]
		public PoolOfAcid( TimeSpan duration, int minDamage, int maxDamage )
			: base( 0x122A )
		{
			Hue = 0x3F;
			Movable = false;

			m_MinDamage = minDamage;
			m_MaxDamage = maxDamage;
			m_Created = DateTime.Now;
			m_Duration = duration;

			m_Timer = Timer.DelayCall( TimeSpan.Zero, TimeSpan.FromSeconds( 1 ), new TimerCallback( OnTick ) );
		}

		public override void OnAfterDelete()
		{
			if( m_Timer != null )
				m_Timer.Stop();
		}

		private void OnTick()
		{
			DateTime now = DateTime.Now;
			TimeSpan age = now - m_Created;

			if( age > m_Duration )
				Delete();
			else
			{
				if( !Drying && age > (m_Duration - age) )
					Drying = true;

				ArrayList toDamage = new ArrayList();

				foreach( Mobile m in GetMobilesInRange( 0 ) )
				{
					BaseCreature bc = m as BaseCreature;

					if( m.Alive && !m.IsDeadBondedPet && (bc == null || bc.Controlled || bc.Summoned) )
					{
						toDamage.Add( m );
					}
				}

				for( int i = 0; i < toDamage.Count; i++ )
					Damage( (Mobile)toDamage[i] );

			}
		}


		public override bool OnMoveOver( Mobile m )
		{
			Damage( m );
			return true;
		}

		public void Damage( Mobile m )
		{
			m.Damage( Utility.RandomMinMax( MinDamage, MaxDamage ) );
		}

		public PoolOfAcid( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			//Don't serialize these
		}

		public override void Deserialize( GenericReader reader )
		{
		}
	}
}
