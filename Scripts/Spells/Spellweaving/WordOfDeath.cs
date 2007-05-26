using System;
using Server.Targeting;
using Server.Network;

namespace Server.Spells.Spellweaving
{
	public class WordOfDeathSpell : ArcanistSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
                "Word of Death", "Nyraxle",
				-1
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 3.5 ); } }

        public override double RequiredSkill { get { return 80.0; } }
        public override int RequiredMana { get { return 50; } }

        public WordOfDeathSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( Mobile m )
		{
			if( !Caster.CanSee( m ) )
			{
				Caster.SendLocalizedMessage( 500237 ); // Target can not be seen.
			}
			else if( CheckHSequence( m ) )
			{
				Point3D loc = m.Location;
				loc.Z += 50;

				m.PlaySound( 0x211 );
				m.FixedParticles( 0x3779, 1, 30, 0x26EC, 0x3, 0x3, EffectLayer.Waist );

				Effects.SendMovingParticles( new Entity( Serial.Zero, loc, m.Map ), new Entity( Serial.Zero, m.Location, m.Map ), 0xF5F, 1, 0, true, false, 0x21, 0x3F, 0x251D, 0, 0, EffectLayer.Head, 0 );

				int percentage = 5 + (5 * FocusLevel);

				int damage;

				if( !m.Player && ((m.Hits / m.HitsMax)*100) < percentage )
				{
					damage = 300;
				}
				else
				{
					damage = GetNewAosDamage( (int)Math.Max( Caster.Skills.Spellweaving.Value/24, 1 ) + 4, 1, 4, m );
				}

				int[] types = new int[4];
				types[Utility.Random( types.Length )] = 100;

				SpellHelper.Damage( this, m, damage, 0, types[0], types[1], types[2], types[3] );	//Chaos damage.  Random elemental damage
			}

			FinishSequence();
		}

		public class InternalTarget : Target
		{
			private WordOfDeathSpell m_Owner;

			public InternalTarget( WordOfDeathSpell owner ) : base( 12, false, TargetFlags.Harmful )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile m, object o )
			{
				if( o is Mobile )
				{
					m_Owner.Target( (Mobile)o );
				}
			}

			protected override void OnTargetFinish( Mobile m )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}