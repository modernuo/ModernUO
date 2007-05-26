using System;
using System.Collections.Generic;
using Server.Network;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.Fourth
{
	public class ArchCureSpell : MagerySpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Arch Cure", "Vas An Nox",
				215,
				9061,
				Reagent.Garlic,
				Reagent.Ginseng,
				Reagent.MandrakeRoot
			);

		public override SpellCircle Circle { get { return SpellCircle.Fourth; } }

		public ArchCureSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		// Archcure is now 1/4th of a second faster
		public override TimeSpan CastDelayBase{ get{ return base.CastDelayBase - TimeSpan.FromSeconds( 0.25 ); } }

		public void Target( IPoint3D p )
		{
			if ( !Caster.CanSee( p ) )
			{
				Caster.SendLocalizedMessage( 500237 ); // Target can not be seen.
			}
			else if ( CheckSequence() )
			{
				SpellHelper.Turn( Caster, p );

				SpellHelper.GetSurfaceTop( ref p );

				List<Mobile> targets = new List<Mobile>();

				Map map = Caster.Map;

				if ( map != null )
				{
					IPooledEnumerable eable = map.GetMobilesInRange( new Point3D( p ), 2 );

					foreach ( Mobile m in eable )
					{
						// Archcure doesn't cure aggressors or victims 
						if ( Caster.CanBeBeneficial( m, false ) && (!Core.AOS || !IsAggressor( m ) && !IsAggressed( m )) )
							targets.Add( m );
					}

					eable.Free();
				}

				Effects.PlaySound( p, Caster.Map, 0x299 );

				if ( targets.Count > 0 )
				{
					int cured = 0;

					for ( int i = 0; i < targets.Count; ++i )
					{
						Mobile m = targets[i];

						Caster.DoBeneficial( m );

						Poison poison = m.Poison;

						if ( poison != null )
						{
							int chanceToCure = 10000 + (int)(Caster.Skills[SkillName.Magery].Value * 75) - ((poison.Level + 1) * 1750);
							chanceToCure /= 100;
							chanceToCure -= 1;

							if ( chanceToCure > Utility.Random( 100 ) && m.CurePoison( Caster ) )
								++cured;
						}

						m.FixedParticles( 0x373A, 10, 15, 5012, EffectLayer.Waist );
						m.PlaySound( 0x1E0 );
					}

					if ( cured > 0 )
						Caster.SendLocalizedMessage( 1010058 ); // You have cured the target of all poisons!
				}
			}

			FinishSequence();
		}

		private bool IsAggressor( Mobile m )
		{
			foreach ( AggressorInfo info in Caster.Aggressors )
			{
				if ( m == info.Attacker && !info.Expired )
					return true;
			}

			return false;
		}

		private bool IsAggressed( Mobile m )
		{
			foreach ( AggressorInfo info in Caster.Aggressed )
			{
				if ( m == info.Defender && !info.Expired )
					return true;
			}

			return false;
		}

		private class InternalTarget : Target
		{
			private ArchCureSpell m_Owner;

			public InternalTarget( ArchCureSpell owner ) : base( 12, true, TargetFlags.None )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				IPoint3D p = o as IPoint3D;

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