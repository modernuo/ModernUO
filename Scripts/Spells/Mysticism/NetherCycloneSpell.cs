using System;
using System.Collections.Generic;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Mysticism
{
	public class NetherCycloneSpell : MysticSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Nether Cyclone", "Grav Hur",
				-1,
				9002,
				Reagent.MandrakeRoot,
				Reagent.Nightshade,
				Reagent.SulfurousAsh,
				Reagent.Bloodmoss
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 2.5 ); } }

		public override double RequiredSkill { get { return 83.0; } }
		public override int RequiredMana { get { return 50; } }

		public NetherCycloneSpell( Mobile caster, Item scroll )
			: base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( IPoint3D p )
		{
			if ( SpellHelper.CheckTown( p, Caster ) && CheckSequence() )
			{
				/* Summons a gale of lethal winds that strikes all Targets within a radius around
				 * the Target's Location, dealing chaos damage. In addition to inflicting damage,
				 * each Target of the Nether Cyclone temporarily loses a percentage of mana and
				 * stamina. The effectiveness of the Nether Cyclone is determined by a comparison
				 * between the Caster's Mysticism and either Focus or Imbuing (whichever is greater)
				 * skills and the Resisting Spells skill of the Target.
				 */

				SpellHelper.Turn( Caster, p );

				if ( p is Item )
					p = ( (Item) p ).GetWorldLocation();

				var targets = new List<Mobile>();

				var map = Caster.Map;

				var pvp = false;

				if ( map != null )
				{
					PlayEffect( p, Caster.Map );

					foreach ( var m in map.GetMobilesInRange( new Point3D( p ), 2 ) )
					{
						if ( m == Caster )
							continue;

						if ( SpellHelper.ValidIndirectTarget( Caster, m ) && Caster.CanBeHarmful( m, false ) && Caster.CanSee( m ) )
						{
							if ( !Caster.InLOS( m ) )
								continue;

							targets.Add( m );

							if ( m.Player )
								pvp = true;
						}
					}
				}

				var damage = GetNewAosDamage( 51, 1, 5, pvp );
				var reduction = ( GetBaseSkill( Caster ) + GetBoostSkill( Caster ) ) / 1200.0;

				foreach ( var m in targets )
				{
					Caster.DoHarmful( m );

					var types = new int[4];
					types[Utility.Random( types.Length )] = 100;

					SpellHelper.Damage( this, m, damage, 0, types[0], types[1], types[2], types[3] );

					var resistedReduction = reduction - ( m.Skills[SkillName.MagicResist].Value / 800.0 );

					m.Stam -= (int) ( m.StamMax * resistedReduction );
					m.Mana -= (int) ( m.ManaMax * resistedReduction );
				}
			}

			FinishSequence();
		}

		private static void PlayEffect( IPoint3D p, Map map )
		{
			Effects.PlaySound( p, map, 0x64F );

			PlaySingleEffect( p, map, -1, 1, -1, 1 );
			PlaySingleEffect( p, map, -2, 0, -3, -1 );
			PlaySingleEffect( p, map, -3, -1, -1, 1 );
			PlaySingleEffect( p, map, 1, 3, -1, 1 );
			PlaySingleEffect( p, map, -1, 1, 1, 3 );
		}

		private static void PlaySingleEffect( IPoint3D p, Map map, int a, int b, int c, int d )
		{
			int x = p.X, y = p.Y, z = p.Z + 18;

			SendEffectPacket( p, map, new Point3D( x + a, y + c, z ), new Point3D( x + a, y + c, z ) );
			SendEffectPacket( p, map, new Point3D( x + b, y + c, z ), new Point3D( x + b, y + c, z ) );
			SendEffectPacket( p, map, new Point3D( x + b, y + d, z ), new Point3D( x + b, y + d, z ) );
			SendEffectPacket( p, map, new Point3D( x + a, y + d, z ), new Point3D( x + a, y + d, z ) );

			SendEffectPacket( p, map, new Point3D( x + b, y + c, z ), new Point3D( x + a, y + c, z ) );
			SendEffectPacket( p, map, new Point3D( x + b, y + d, z ), new Point3D( x + b, y + c, z ) );
			SendEffectPacket( p, map, new Point3D( x + a, y + d, z ), new Point3D( x + b, y + d, z ) );
			SendEffectPacket( p, map, new Point3D( x + a, y + c, z ), new Point3D( x + a, y + d, z ) );
		}

		private static void SendEffectPacket( IPoint3D p, Map map, Point3D orig, Point3D dest )
		{
			Effects.SendPacket( p, map, new HuedEffect( EffectType.Moving, Serial.Zero, Serial.Zero, 0x375A, orig, dest, 0, 0, false, false, 0x49A, 0x4 ) );
		}

		private class InternalTarget : Target
		{
			private NetherCycloneSpell m_Owner;

			public InternalTarget( NetherCycloneSpell owner )
				: base( 12, true, TargetFlags.None )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				var p = o as IPoint3D;

				if ( p != null )
					m_Owner.Target( p );
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}
