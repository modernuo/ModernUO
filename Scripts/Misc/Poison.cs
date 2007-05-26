using System;
using Server;
using Server.Items;
using Server.Network;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;

namespace Server
{
	public class PoisonImpl : Poison
	{
		[CallPriority( 10 )]
		public static void Configure()
		{
			if ( Core.AOS )
			{
				Register( new PoisonImpl( "Lesser",		0,  4, 16,  7.5, 3.0, 2.25, 10, 4 ) );
				Register( new PoisonImpl( "Regular",	1,  8, 18, 10.0, 3.0, 3.25, 10, 3 ) );
				Register( new PoisonImpl( "Greater",	2, 12, 20, 15.0, 3.0, 4.25, 10, 2 ) );
				Register( new PoisonImpl( "Deadly",		3, 16, 30, 30.0, 3.0, 5.25, 15, 2 ) );
				Register( new PoisonImpl( "Lethal",		4, 20, 50, 35.0, 3.0, 5.25, 20, 2 ) );
			}
			else
			{
				Register( new PoisonImpl( "Lesser",		0, 4, 26,  2.500, 3.5, 3.0, 10, 2 ) );
				Register( new PoisonImpl( "Regular",	1, 5, 26,  3.125, 3.5, 3.0, 10, 2 ) );
				Register( new PoisonImpl( "Greater",	2, 6, 26,  6.250, 3.5, 3.0, 10, 2 ) );
				Register( new PoisonImpl( "Deadly",		3, 7, 26, 12.500, 3.5, 4.0, 10, 2 ) );
				Register( new PoisonImpl( "Lethal",		4, 9, 26, 25.000, 3.5, 5.0, 10, 2 ) );
			}
		}

		public static Poison IncreaseLevel( Poison oldPoison )
		{
			Poison newPoison = ( oldPoison == null ? null : GetPoison( oldPoison.Level + 1 ) );

			return ( newPoison == null ? oldPoison : newPoison );
		}

		// Info
		private string m_Name;
		private int m_Level;

		// Damage
		private int m_Minimum, m_Maximum;
		private double m_Scalar;

		// Timers
		private TimeSpan m_Delay;
		private TimeSpan m_Interval;
		private int m_Count, m_MessageInterval;

		public PoisonImpl( string name, int level, int min, int max, double percent, double delay, double interval, int count, int messageInterval )
		{
			m_Name = name;
			m_Level = level;
			m_Minimum = min;
			m_Maximum = max;
			m_Scalar = percent * 0.01;
			m_Delay = TimeSpan.FromSeconds( delay );
			m_Interval = TimeSpan.FromSeconds( interval );
			m_Count = count;
			m_MessageInterval = messageInterval;
		}

		public override string Name{ get{ return m_Name; } }
		public override int Level{ get{ return m_Level; } }

		public class PoisonTimer : Timer
		{
			private PoisonImpl m_Poison;
			private Mobile m_Mobile;
			private Mobile m_From;
			private int m_LastDamage;
			private int m_Index;

			public Mobile From{ get{ return m_From; } set{ m_From = value; } }

			public PoisonTimer( Mobile m, PoisonImpl p ) : base( p.m_Delay, p.m_Interval )
			{
				m_From = m;
				m_Mobile = m;
				m_Poison = p;
			}

			protected override void OnTick()
			{
				if ( (Core.AOS && m_Poison.Level < 4 && TransformationSpellHelper.UnderTransformation( m_Mobile, typeof( VampiricEmbraceSpell ) )) ||
					(m_Poison.Level < 3 && OrangePetals.UnderEffect( m_Mobile )) ||
					AnimalForm.UnderTransformation( m_Mobile, typeof( Unicorn ) ) )
				{
					if ( m_Mobile.CurePoison( m_Mobile ) )
					{
						m_Mobile.LocalOverheadMessage( MessageType.Emote, 0x3F, true,
							"* You feel yourself resisting the effects of the poison *" );

						m_Mobile.NonlocalOverheadMessage( MessageType.Emote, 0x3F, true,
							String.Format( "* {0} seems resistant to the poison *", m_Mobile.Name ) );

						Stop();
						return;
					}
				}

				if ( m_Index++ == m_Poison.m_Count )
				{
					m_Mobile.SendLocalizedMessage( 502136 ); // The poison seems to have worn off.
					m_Mobile.Poison = null;

					Stop();
					return;
				}

				int damage;

				if ( !Core.AOS && m_LastDamage != 0 && Utility.RandomBool() )
				{
					damage = m_LastDamage;
				}
				else
				{
					damage = 1 + (int)(m_Mobile.Hits * m_Poison.m_Scalar);

					if ( damage < m_Poison.m_Minimum )
						damage = m_Poison.m_Minimum;
					else if ( damage > m_Poison.m_Maximum )
						damage = m_Poison.m_Maximum;

					m_LastDamage = damage;
				}

				if ( m_From != null )
					m_From.DoHarmful( m_Mobile, true );

				IHonorTarget honorTarget = m_Mobile as IHonorTarget;
				if ( honorTarget != null && honorTarget.ReceivedHonorContext != null )
					honorTarget.ReceivedHonorContext.OnTargetPoisoned();

				AOS.Damage( m_Mobile, m_From, damage, 0, 0, 0, 100, 0 );

				if ( (m_Index % m_Poison.m_MessageInterval) == 0 )
					m_Mobile.OnPoisoned( m_From, m_Poison, m_Poison );
			}
		}

		public override Timer ConstructTimer( Mobile m )
		{
			return new PoisonTimer( m, this );
		}
	}
}