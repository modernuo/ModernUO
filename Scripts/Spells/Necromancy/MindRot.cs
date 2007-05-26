using System;
using System.Collections;
using Server.Network;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
	public class MindRotSpell : NecromancerSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Mind Rot", "Wis An Ben",
				203,
				9031,
				Reagent.BatWing,
				Reagent.PigIron,
				Reagent.DaemonBlood
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.5 ); } }

		public override double RequiredSkill{ get{ return 30.0; } }
		public override int RequiredMana{ get{ return 17; } }

		public MindRotSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( Mobile m )
		{
			if ( HasMindRotScalar( m ) )
			{
				Caster.SendLocalizedMessage( 1005559 ); // This spell is already in effect.
			}
			else if ( CheckHSequence( m ) )
			{
				SpellHelper.Turn( Caster, m );

				/* Attempts to place a curse on the Target that increases the mana cost of any spells they cast,
				 * for a duration based off a comparison between the Caster's Spirit Speak skill and the Target's Resisting Spells skill.
				 * The effect lasts for ((Spirit Speak skill level - target's Resist Magic skill level) / 50 ) + 20 seconds.
				 */

				m.PlaySound( 0x1FB );
				m.PlaySound( 0x258 );
				m.FixedParticles( 0x373A, 1, 17, 9903, 15, 4, EffectLayer.Head );

				TimeSpan duration = TimeSpan.FromSeconds( (((GetDamageSkill( Caster ) - GetResistSkill( m )) / 5.0) + 20.0) * (m.Player ? 1.0 : 2.0 ) );
				m.CheckSkill( SkillName.MagicResist, 0.0, 120.0 );	//Skill check for gain

				if ( m.Player )
					SetMindRotScalar( Caster, m, 1.25, duration );
				else
					SetMindRotScalar( Caster, m, 2.00, duration );
			}

			FinishSequence();
		}

		private static Hashtable m_Table = new Hashtable();

		public static void ClearMindRotScalar( Mobile m )
		{
			m_Table.Remove( m );
			BuffInfo.RemoveBuff( m, BuffIcon.Mindrot );
		}

		public static bool HasMindRotScalar( Mobile m )
		{
			return m_Table.Contains( m );
		}

		public static bool GetMindRotScalar( Mobile m, ref double scalar )
		{
			object obj = m_Table[m];

			if ( obj == null )
				return false;

			scalar = (double)obj;
			return true;
		}

		public static void SetMindRotScalar( Mobile caster, Mobile target, double scalar, TimeSpan duration )
		{
			m_Table[target] = scalar;
			BuffInfo.AddBuff( target, new BuffInfo( BuffIcon.Mindrot, 1075665, duration, target  ) );
			new ExpireTimer( caster, target, duration ).Start();
		}

		private class ExpireTimer : Timer
		{
			private Mobile m_Caster;
			private Mobile m_Target;
			private DateTime m_End;

			public ExpireTimer( Mobile caster, Mobile target, TimeSpan delay ) : base( TimeSpan.FromSeconds( 1.0 ), TimeSpan.FromSeconds( 1.0 ) )
			{
				m_Caster = caster;
				m_Target = target;
				m_End = DateTime.Now + delay;

				Priority = TimerPriority.TwoFiftyMS;
			}

			protected override void OnTick()
			{
				if ( m_Target.Deleted || !m_Target.Alive || DateTime.Now >= m_End )
				{
					m_Target.SendLocalizedMessage( 1060872 ); // Your mind feels normal again.
					ClearMindRotScalar( m_Target );
					Stop();
				}
			}
		}

		private class InternalTarget : Target
		{
			private MindRotSpell m_Owner;

			public InternalTarget( MindRotSpell owner ) : base( 12, false, TargetFlags.Harmful )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is Mobile )
					m_Owner.Target( (Mobile) o );
				else
					from.SendLocalizedMessage( 1060508 ); // You can't curse that.
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}