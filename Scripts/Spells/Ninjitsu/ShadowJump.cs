using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Ninjitsu
{
	public class Shadowjump : NinjaSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
			"Shadowjump", null,
			-1,
			9002
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.0 ); } }

		public override double RequiredSkill{ get{ return 50.0; } }
		public override int RequiredMana{ get{ return 15; } }

		public override bool BlockedByAnimalForm{ get{ return false; } }

		public Shadowjump( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override bool CheckCast()
		{
			if ( !Caster.Hidden || Caster.AllowedStealthSteps <= 0 )
			{
				Caster.SendLocalizedMessage( 1063087 ); // You must be in stealth mode to use this ability.
				return false;
			}

			return base.CheckCast();
		}

		public override bool CheckDisturb( DisturbType type, bool firstCircle, bool resistable )
		{
			return false;
		}

		public override void OnCast()
		{
			Caster.SendLocalizedMessage( 1063088 ); // You prepare to perform a Shadowjump.
			Caster.Target = new InternalTarget( this );
		}

		public void Target( IPoint3D p )
		{
			IPoint3D orig = p;
			Map map = Caster.Map;

			SpellHelper.GetSurfaceTop( ref p );

			if ( !Caster.Hidden || Caster.AllowedStealthSteps <= 0 )
			{
				Caster.SendLocalizedMessage( 1063087 ); // You must be in stealth mode to use this ability.
			}
			else if ( Factions.Sigil.ExistsOn( Caster ) )
			{
				Caster.SendLocalizedMessage( 1061632 ); // You can't do that while carrying the sigil.
			}
			else if ( Server.Misc.WeightOverloading.IsOverloaded( Caster ) )
			{
				Caster.SendLocalizedMessage( 502359, "", 0x22 ); // Thou art too encumbered to move.
			}
			else if ( !SpellHelper.CheckTravel( Caster, TravelCheckType.TeleportFrom ) || !SpellHelper.CheckTravel( Caster, map, new Point3D( p ), TravelCheckType.TeleportTo ))
			{
			}
			else if ( map == null || !map.CanSpawnMobile( p.X, p.Y, p.Z ) )
			{
				Caster.SendLocalizedMessage( 502831 ); // Cannot teleport to that spot.
			}
			else if ( SpellHelper.CheckMulti( new Point3D( p ), map ) )
			{
				// TODO: Cannot shadowjump within 5 tiles from any house.
				Caster.SendLocalizedMessage( 502831 ); // Cannot teleport to that spot.
			}
			else if ( CheckSequence() )
			{
				SpellHelper.Turn( Caster, orig );

				Mobile m = Caster;

				Point3D from = m.Location;
				Point3D to = new Point3D( p );

				m.Location = to;
				m.ProcessDelta();

				Effects.SendLocationParticles( EffectItem.Create( from, m.Map, EffectItem.DefaultDuration ), 0x3728, 10, 10, 2023 );

				m.PlaySound( 0x512 );
			}

			FinishSequence();
		}

		public class InternalTarget : Target
		{
			private Shadowjump m_Owner;

			public InternalTarget( Shadowjump owner ) : base( 11, true, TargetFlags.None )
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

				if ( !from.CheckSkill( SkillName.Hiding, 0.0, 100.0 ) )	//TODO: Hiding check or stealth check?
					from.RevealingAction();
			}
		}
	}
}