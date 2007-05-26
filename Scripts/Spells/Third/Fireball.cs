using System;
using Server.Targeting;
using Server.Network;

namespace Server.Spells.Third
{
	public class FireballSpell : MagerySpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Fireball", "Vas Flam",
				203,
				9041,
				Reagent.BlackPearl
			);

		public override SpellCircle Circle { get { return SpellCircle.Third; } }

		public FireballSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public override bool DelayedDamage{ get{ return true; } }

		public void Target( Mobile m )
		{
			if ( !Caster.CanSee( m ) )
			{
				Caster.SendLocalizedMessage( 500237 ); // Target can not be seen.
			}
			else if ( CheckHSequence( m ) )
			{
				Mobile source = Caster;

				SpellHelper.Turn( source, m );

				SpellHelper.CheckReflect( (int)this.Circle, ref source, ref m );

				double damage;

				if ( Core.AOS )
				{
					damage = GetNewAosDamage( 19, 1, 5, m );
				}
				else
				{
					damage = Utility.Random( 10, 7 );

					if ( CheckResisted( m ) )
					{
						damage *= 0.75;

						m.SendLocalizedMessage( 501783 ); // You feel yourself resisting magical energy.
					}

					damage *= GetDamageScalar( m );
				}

				source.MovingParticles( m, 0x36D4, 7, 0, false, true, 9502, 4019, 0x160 );
				source.PlaySound( Core.AOS ? 0x15E : 0x44B );

				SpellHelper.Damage( this, m, damage, 0, 100, 0, 0, 0 );
			}

			FinishSequence();
		}

		private class InternalTarget : Target
		{
			private FireballSpell m_Owner;

			public InternalTarget( FireballSpell owner ) : base( 12, false, TargetFlags.Harmful )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is Mobile )
					m_Owner.Target( (Mobile)o );
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}