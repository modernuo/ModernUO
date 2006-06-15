using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
	public class PoisonStrikeSpell : NecromancerSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Poison Strike", "In Vas Nox",
				SpellCircle.Fourth, // 0.5 + 1.0 = 1.5s base cast delay
				203,
				9031,
				Reagent.NoxCrystal
			);

		public override double RequiredSkill{ get{ return 50.0; } }
		public override int RequiredMana{ get{ return 17; } }

		public PoisonStrikeSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public override bool DelayedDamage{ get{ return false; } }

		public void Target( Mobile m )
		{
			if ( CheckHSequence( m ) )
			{
				SpellHelper.Turn( Caster, m );

				/* Creates a blast of poisonous energy centered on the target.
				 * The main target is inflicted with a large amount of Poison damage, and all valid targets in a radius of 2 tiles around the main target are inflicted with a lesser effect.
				 * One tile from main target receives 50% damage, two tiles from target receives 33% damage.
				 */

				CheckResisted( m ); // Check magic resist for skill, but do not use return value

				Effects.SendLocationParticles( EffectItem.Create( m.Location, m.Map, EffectItem.DefaultDuration ), 0x36B0, 1, 14, 63, 7, 9915, 0 );
				Effects.PlaySound( m.Location, m.Map, 0x229 );

				double damage = Utility.RandomMinMax( 36, 40 ) * ((300 + (GetDamageSkill( Caster ) * 9)) / 1000);

				Map map = m.Map;

				if ( map != null )
				{
					ArrayList targets = new ArrayList();

					foreach ( Mobile targ in m.GetMobilesInRange( 2 ) )
					{
						if ( (Caster == targ || SpellHelper.ValidIndirectTarget( Caster, targ )) && Caster.CanBeHarmful( targ, false ) )
							targets.Add( targ );
					}

					for ( int i = 0; i < targets.Count; ++i )
					{
						Mobile targ = (Mobile)targets[i];

						int num;

						if ( targ.InRange( m.Location, 0 ) )
							num = 1;
						else if ( targ.InRange( m.Location, 1 ) )
							num = 2;
						else
							num = 3;

						Caster.DoHarmful( targ );
						SpellHelper.Damage( this, targ, damage / num, 0, 0, 0, 100, 0 );
					}
				}
			}

			FinishSequence();
		}

		private class InternalTarget : Target
		{
			private PoisonStrikeSpell m_Owner;

			public InternalTarget( PoisonStrikeSpell owner ) : base( 12, false, TargetFlags.Harmful )
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