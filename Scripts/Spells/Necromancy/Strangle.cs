using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
	public class StrangleSpell : NecromancerSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Strangle", "In Bal Nox",
				209,
				9031,
				Reagent.DaemonBlood,
				Reagent.NoxCrystal
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 2.0 ); } }

		public override double RequiredSkill{ get{ return 65.0; } }
		public override int RequiredMana{ get{ return 29; } }

		public StrangleSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( Mobile m )
		{
			if ( CheckHSequence( m ) )
			{
				SpellHelper.Turn( Caster, m );

				//SpellHelper.CheckReflect( (int)this.Circle, Caster, ref m );	//Irrelevent after AoS

				/* Temporarily chokes off the air suply of the target with poisonous fumes.
				 * The target is inflicted with poison damage over time.
				 * The amount of damage dealt each "hit" is based off of the caster's Spirit Speak skill and the Target's current Stamina.
				 * The less Stamina the target has, the more damage is done by Strangle.
				 * Duration of the effect is Spirit Speak skill level / 10 rounds, with a minimum number of 4 rounds.
				 * The first round of damage is dealt after 5 seconds, and every next round after that comes 1 second sooner than the one before, until there is only 1 second between rounds.
				 * The base damage of the effect lies between (Spirit Speak skill level / 10) - 2 and (Spirit Speak skill level / 10) + 1.
				 * Base damage is multiplied by the following formula: (3 - (target's current Stamina / target's maximum Stamina) * 2).
				 * Example:
				 * For a target at full Stamina the damage multiplier is 1,
				 * for a target at 50% Stamina the damage multiplier is 2 and
				 * for a target at 20% Stamina the damage multiplier is 2.6
				 */

				m.PlaySound( 0x22F );
				m.FixedParticles( 0x36CB, 1, 9, 9911, 67, 5, EffectLayer.Head );
				m.FixedParticles( 0x374A, 1, 17, 9502, 1108, 4, (EffectLayer)255 );

				if ( !m_Table.Contains( m ) )
				{
					Timer t = new InternalTimer( m, Caster );
					t.Start();

					m_Table[m] = t;
				}
			}

			FinishSequence();
		}

		private static Hashtable m_Table = new Hashtable();

		public static bool RemoveCurse( Mobile m )
		{
			Timer t = (Timer)m_Table[m];

			if ( t == null )
				return false;

			t.Stop();
			m.SendLocalizedMessage( 1061687 ); // You can breath normally again.

			m_Table.Remove( m );
			return true;
		}

		private class InternalTimer : Timer
		{
			private Mobile m_Target, m_From;
			private double m_MinBaseDamage, m_MaxBaseDamage;

			private DateTime m_NextHit;
			private int m_HitDelay;

			private int m_Count, m_MaxCount;

			public InternalTimer( Mobile target, Mobile from ) : base( TimeSpan.FromSeconds( 0.1 ), TimeSpan.FromSeconds( 0.1 ) )
			{
				Priority = TimerPriority.FiftyMS;

				m_Target = target;
				m_From = from;

				double spiritLevel = from.Skills[SkillName.SpiritSpeak].Value / 10;

				m_MinBaseDamage = spiritLevel - 2;
				m_MaxBaseDamage = spiritLevel + 1;

				m_HitDelay = 5;
				m_NextHit = DateTime.Now + TimeSpan.FromSeconds( m_HitDelay );

				m_Count = (int)spiritLevel;

				if ( m_Count < 4 )
					m_Count = 4;

				m_MaxCount = m_Count;
			}

			protected override void OnTick()
			{
				if ( !m_Target.Alive )
				{
					m_Table.Remove( m_Target );
					Stop();
				}

				if ( !m_Target.Alive || DateTime.Now < m_NextHit )
					return;

				--m_Count;

				if ( m_HitDelay > 1 )
				{
					if ( m_MaxCount < 5 )
					{
						--m_HitDelay;
					}
					else
					{
						int delay = (int)(Math.Ceiling( (1.0 + (5 * m_Count)) / m_MaxCount ) );

						if ( delay <= 5 )
							m_HitDelay = delay;
						else
							m_HitDelay = 5;
					}
				}

				if ( m_Count == 0 )
				{
					m_Target.SendLocalizedMessage( 1061687 ); // You can breath normally again.
					m_Table.Remove( m_Target );
					Stop();
				}
				else
				{
					m_NextHit = DateTime.Now + TimeSpan.FromSeconds( m_HitDelay );

					double damage = m_MinBaseDamage + (Utility.RandomDouble() * (m_MaxBaseDamage - m_MinBaseDamage));

					damage *= (3 - (((double)m_Target.Stam / m_Target.StamMax) * 2));

					if ( damage < 1 )
						damage = 1;

					if ( !m_Target.Player )
						damage *= 1.75;

					AOS.Damage( m_Target, m_From, (int)damage, 0, 0, 0, 100, 0 );
				}
			}
		}

		private class InternalTarget : Target
		{
			private StrangleSpell m_Owner;

			public InternalTarget( StrangleSpell owner ) : base( 12, false, TargetFlags.Harmful )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is Mobile )
					m_Owner.Target( (Mobile) o );
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}