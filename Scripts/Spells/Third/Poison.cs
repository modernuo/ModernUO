using System;
using Server.Targeting;
using Server.Network;

namespace Server.Spells.Third
{
	public class PoisonSpell : MagerySpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Poison", "In Nox",
				203,
				9051,
				Reagent.Nightshade
			);

		public override SpellCircle Circle { get { return SpellCircle.Third; } }

		public PoisonSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( Mobile m )
		{
			if ( !Caster.CanSee( m ) )
			{
				Caster.SendLocalizedMessage( 500237 ); // Target can not be seen.
			}
			else if ( CheckHSequence( m ) )
			{
				SpellHelper.Turn( Caster, m );

				SpellHelper.CheckReflect( (int)this.Circle, Caster, ref m );

				if ( m.Spell != null )
					m.Spell.OnCasterHurt();

				m.Paralyzed = false;

				if ( CheckResisted( m ) )
				{
					m.SendLocalizedMessage( 501783 ); // You feel yourself resisting magical energy.
				}
				else
				{
					int level;

					if ( Core.AOS )
					{
						if ( Caster.InRange( m, 2 ) )
						{
							int total = (Caster.Skills.Magery.Fixed + Caster.Skills.Poisoning.Fixed) / 2;

							if ( total >= 1000 )
								level = 3;
							else if ( total > 850 )
								level = 2;
							else if ( total > 650 )
								level = 1;
							else
								level = 0;
						}
						else
						{
							level = 0;
						}
					}
					else
					{
						double total = Caster.Skills[SkillName.Magery].Value + Caster.Skills[SkillName.Poisoning].Value;

						double dist = Caster.GetDistanceToSqrt( m );

						if ( dist >= 3.0 )
							total -= (dist - 3.0) * 10.0;

						if ( total >= 200.0 && 1 > Utility.Random( 10 ) )
							level = 3;
						else if ( total > (Core.AOS ? 170.1 : 170.0) )
							level = 2;
						else if ( total > (Core.AOS ? 130.1 : 130.0) )
							level = 1;
						else
							level = 0;
					}

					m.ApplyPoison( Caster, Poison.GetPoison( level ) );
				}

				m.FixedParticles( 0x374A, 10, 15, 5021, EffectLayer.Waist );
				m.PlaySound( 0x474 );
			}

			FinishSequence();
		}

		private class InternalTarget : Target
		{
			private PoisonSpell m_Owner;

			public InternalTarget( PoisonSpell owner ) : base( 12, false, TargetFlags.Harmful )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is Mobile )
				{
					m_Owner.Target( (Mobile)o );
				}
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}