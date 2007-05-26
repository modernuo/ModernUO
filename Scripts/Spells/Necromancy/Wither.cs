using System;
using System.Collections.Generic;
using Server.Network;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
	public class WitherSpell : NecromancerSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Wither", "Kal Vas An Flam",
				203,
				9031,
				Reagent.NoxCrystal,
				Reagent.GraveDust,
				Reagent.PigIron
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.25 ); } }

		public override double RequiredSkill{ get{ return 60.0; } }
		public override int RequiredMana{ get{ return 23; } }

		public WitherSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override bool DelayedDamage{ get{ return false; } }

		public override void OnCast()
		{
			if ( CheckSequence() )
			{
				/* Creates a withering frost around the Caster,
				 * which deals Cold Damage to all valid targets in a radius of 5 tiles.
				 */

				Map map = Caster.Map;

				if ( map != null )
				{
					List<Mobile> targets = new List<Mobile>();

					foreach ( Mobile m in Caster.GetMobilesInRange( 5 ) )
						if ( Caster != m && Caster.InLOS( m ) && SpellHelper.ValidIndirectTarget( Caster, m ) && Caster.CanBeHarmful( m, false ) )
							targets.Add( m );

					Effects.PlaySound( Caster.Location, map, 0x1FB );
					Effects.PlaySound( Caster.Location, map, 0x10B );
					Effects.SendLocationParticles( EffectItem.Create( Caster.Location, map, EffectItem.DefaultDuration ), 0x37CC, 1, 40, 97, 3, 9917, 0 );

					for ( int i = 0; i < targets.Count; ++i )
					{
						Mobile m = targets[i];

						Caster.DoHarmful( m );
						m.FixedParticles( 0x374A, 1, 15, 9502, 97, 3, (EffectLayer)255 );

						double damage = Utility.RandomMinMax( 30, 35 );

						damage *= (300 + (m.Karma / 100) + (GetDamageSkill( Caster ) * 10));
						damage /= 1000;

						// TODO: cap?
						//if ( damage > 40 )
						//	damage = 40;

						SpellHelper.Damage( this, m, damage, 0, 0, 100, 0, 0 );
					}
				}
			}

			FinishSequence();
		}
	}
}