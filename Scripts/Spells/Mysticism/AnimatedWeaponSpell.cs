using System;
using Server.Targeting;
using Server.Mobiles;

namespace Server.Spells.Mysticism
{
	public class AnimatedWeaponSpell : MysticSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Animated Weapon", "In Jux Por Ylem",
				-1,
				9002,
				Reagent.Bone,
				Reagent.BlackPearl,
				Reagent.MandrakeRoot,
				Reagent.Nightshade
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.5 ); } }

		public override double RequiredSkill { get { return 33.0; } }
		public override int RequiredMana { get { return 11; } }

		public AnimatedWeaponSpell( Mobile caster, Item scroll )
			: base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			Caster.Target = new InternalTarget( this );
		}

		public void Target( IPoint3D p )
		{
			if ( ( Caster.Followers + 4 ) > Caster.FollowersMax )
			{
				Caster.SendLocalizedMessage( 1049645 ); // You have too many followers to summon that creature.
				return;
			}

			var map = Caster.Map;

			SpellHelper.GetSurfaceTop( ref p );

			if ( map == null || ( Caster.Player && !map.CanSpawnMobile( p.X, p.Y, p.Z ) ) )
			{
				Caster.SendLocalizedMessage( 501942 ); // That location is blocked.
			}
			else if ( SpellHelper.CheckTown( p, Caster ) && CheckSequence() )
			{
				var level = (int) ( ( GetBaseSkill( Caster ) + GetBoostSkill( Caster ) ) / 2.0 );

				var duration = TimeSpan.FromSeconds( 10 + level );

				var summon = new AnimatedWeapon( Caster, level );
				BaseCreature.Summon( summon, false, Caster, new Point3D( p ), 0x212, duration );

				summon.PlaySound( 0x64A );

				Effects.SendTargetParticles( summon, 0x3728, 10, 10, 0x13AA, (EffectLayer) 255 );
			}

			FinishSequence();
		}

		public class InternalTarget : Target
		{
			private AnimatedWeaponSpell m_Owner;

			public InternalTarget( AnimatedWeaponSpell owner )
				: base( 12, true, TargetFlags.None )
			{
				m_Owner = owner;
			}

			protected override void OnTarget( Mobile from, object o )
			{
				if ( o is IPoint3D )
					m_Owner.Target( (IPoint3D) o );
			}

			protected override void OnTargetFinish( Mobile from )
			{
				m_Owner.FinishSequence();
			}
		}
	}
}
