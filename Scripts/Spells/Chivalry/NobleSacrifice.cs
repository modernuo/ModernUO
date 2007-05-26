using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Network;
using Server.Items;
using Server.Targeting;
using Server.Gumps;
using Server.Spells.Necromancy;
using Server.Regions;

namespace Server.Spells.Chivalry
{
	public class NobleSacrificeSpell : PaladinSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Noble Sacrifice", "Dium Prostra",
				-1,
				9002
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.5 ); } }

		public override double RequiredSkill{ get{ return 65.0; } }
		public override int RequiredMana{ get{ return 20; } }
		public override int RequiredTithing{ get{ return 30; } }
		public override int MantraNumber{ get{ return 1060725; } } // Dium Prostra
		public override bool BlocksMovement{ get{ return false; } }

		public NobleSacrificeSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			if ( CheckSequence() )
			{
				List<Mobile> targets = new List<Mobile>();

				foreach ( Mobile m in Caster.GetMobilesInRange( 3 ) ) // TODO: Validate range
				{
					if ( m is BaseCreature && ((BaseCreature)m).IsAnimatedDead )
						continue;

					if ( Caster != m && Caster.CanBeBeneficial( m, false, true ) && !(m is Golem) )
						targets.Add( m );
				}

				Caster.PlaySound( 0x244 );
				Caster.FixedParticles( 0x3709, 1, 30, 9965, 5, 7, EffectLayer.Waist );
				Caster.FixedParticles( 0x376A, 1, 30, 9502, 5, 3, EffectLayer.Waist );

				/* Attempts to Resurrect, Cure and Heal all targets in a radius around the caster.
				 * If any target is successfully assisted, the Paladin's current
				 * Hit Points, Mana and Stamina are set to 1.
				 * Amount of damage healed is affected by the Caster's Karma, from 8 to 24 hit points.
				 */

				bool sacrifice = false;

				// TODO: Is there really a resurrection chance?
				double resChance = 0.1 + (0.9 * ((double)Caster.Karma / 10000));

				for ( int i = 0; i < targets.Count; ++i )
				{
					Mobile m = targets[i];

					if ( !m.Alive )
					{
						if ( m.Region != null && m.Region.IsPartOf( "Khaldun" ) )
						{
							Caster.SendLocalizedMessage( 1010395 ); // The veil of death in this area is too strong and resists thy efforts to restore life.
						}
						else if( Core.ML && m.Region != null && m.Region.IsPartOf( typeof( HouseRegion ) ) )
						{
						}
						else if( resChance > Utility.RandomDouble() )
						{
							m.FixedParticles( 0x375A, 1, 15, 5005, 5, 3, EffectLayer.Head );
							m.CloseGump( typeof( ResurrectGump ) );
							m.SendGump( new ResurrectGump( m, Caster ) );
							sacrifice = true;
						}
					}
					else
					{
						bool sendEffect = false;

						if ( m.Poisoned && m.CurePoison( Caster ) )
						{
							Caster.DoBeneficial( m );

							if ( Caster != m )
								Caster.SendLocalizedMessage( 1010058 ); // You have cured the target of all poisons!

							m.SendLocalizedMessage( 1010059 ); // You have been cured of all poisons.
							sendEffect = true;
							sacrifice = true;
						}

						if ( m.Hits < m.HitsMax )
						{
							int toHeal = ComputePowerValue( 10 ) + Utility.RandomMinMax( 0, 2 );

							// TODO: Should caps be applied?
							if ( toHeal < 8 )
								toHeal = 8;
							else if ( toHeal > 24 )
								toHeal = 24;

							Caster.DoBeneficial( m );
							m.Heal( toHeal, Caster );
							sendEffect = true;
						}

						StatMod mod;

						mod = m.GetStatMod( "[Magic] Str Offset" );
						if ( mod != null && mod.Offset < 0 )
						{
							m.RemoveStatMod( "[Magic] Str Offset" );
							sendEffect = true;
						}

						mod = m.GetStatMod( "[Magic] Dex Offset" );
						if ( mod != null && mod.Offset < 0 )
						{
							m.RemoveStatMod( "[Magic] Dex Offset" );
							sendEffect = true;
						}

						mod = m.GetStatMod( "[Magic] Int Offset" );
						if ( mod != null && mod.Offset < 0 )
						{
							m.RemoveStatMod( "[Magic] Int Offset" );
							sendEffect = true;
						}

						if ( m.Paralyzed )
						{
							m.Paralyzed = false;
							sendEffect = true;
						}

						if ( EvilOmenSpell.CheckEffect( m ) )
							sendEffect = true;

						if ( StrangleSpell.RemoveCurse( m ) )
							sendEffect = true;

						if ( CorpseSkinSpell.RemoveCurse( m ) )
							sendEffect = true;

						// TODO: Should this remove blood oath? Pain spike?

						if ( sendEffect )
						{
							m.FixedParticles( 0x375A, 1, 15, 5005, 5, 3, EffectLayer.Head );
							sacrifice = true;
						}
					}
				}

				if ( sacrifice )
				{
					Caster.PlaySound( 0x423 );
					Caster.Hits = 1;
					Caster.Stam = 1;
					Caster.Mana = 1;
				}
			}

			FinishSequence();
		}
	}
}