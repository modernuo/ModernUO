using System;
using System.Collections;
using System.Collections.Generic;
using Server.Items;

namespace Server.Spells.Spellweaving
{
	public class ImmolatingWeaponSpell : ArcanistSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Immolating Weapon", "Thalshara",
				-1
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 1.0 ); } }

		public override double RequiredSkill { get { return 10.0; } }
		public override int RequiredMana { get { return 32; } }

		public ImmolatingWeaponSpell( Mobile caster, Item scroll )
			: base( caster, scroll, m_Info )
		{
		}

		public override bool CheckCast()
		{
			BaseWeapon weapon = Caster.Weapon as BaseWeapon;

			if ( weapon == null || weapon is Fists || weapon is BaseRanged )
			{
				Caster.SendLocalizedMessage( 1060179 ); // You must be wielding a weapon to use this ability!
				return false;
			}

			return base.CheckCast();
		}

		public override void OnCast()
		{
			BaseWeapon weapon = Caster.Weapon as BaseWeapon;

			if ( weapon == null || weapon is Fists || weapon is BaseRanged )
			{
				Caster.SendLocalizedMessage( 1060179 ); // You must be wielding a weapon to use this ability!
			}
			else if ( CheckSequence() )
			{
				Caster.PlaySound( 0x5CA );
				Caster.FixedParticles( 0x36BD, 20, 10, 5044, EffectLayer.Head );

				if ( !IsImmolating( weapon ) ) // On OSI, the effect is not re-applied
				{
					double skill = Caster.Skills.Spellweaving.Value;

					int duration = 10 + (int)( skill / 24 ) + FocusLevel;
					int damage = 5 + (int)( skill / 24 ) + FocusLevel;

					Timer stopTimer = Timer.DelayCall<BaseWeapon>( TimeSpan.FromSeconds( duration ), StopImmolating, weapon );

					m_WeaponDamageTable[weapon] = new ImmolatingWeaponEntry( damage, stopTimer, Caster );
					weapon.InvalidateProperties();
				}
			}

			FinishSequence();
		}

		private static Dictionary<BaseWeapon, ImmolatingWeaponEntry> m_WeaponDamageTable = new Dictionary<BaseWeapon, ImmolatingWeaponEntry>();

		public static bool IsImmolating( BaseWeapon weapon )
		{
			return m_WeaponDamageTable.ContainsKey( weapon );
		}

		public static int GetImmolatingDamage( BaseWeapon weapon )
		{
			ImmolatingWeaponEntry entry;

			if ( m_WeaponDamageTable.TryGetValue( weapon, out entry ) )
				return entry.m_Damage;

			return 0;
		}

		public static void DoEffect( BaseWeapon weapon, Mobile target )
		{
			Timer.DelayCall<DelayedEffectEntry>( TimeSpan.FromSeconds( 0.25 ), FinishEffect, new DelayedEffectEntry( weapon, target ) );
		}

		private static void FinishEffect( DelayedEffectEntry effect )
		{
			ImmolatingWeaponEntry entry;

			if ( m_WeaponDamageTable.TryGetValue( effect.m_Weapon, out entry ) )
				AOS.Damage( effect.m_Target, entry.m_Caster, entry.m_Damage, 0, 100, 0, 0, 0 );
		}

		public static void StopImmolating( BaseWeapon weapon )
		{
			ImmolatingWeaponEntry entry;

			if ( m_WeaponDamageTable.TryGetValue( weapon, out entry ) )
			{
				if ( entry.m_Caster != null )
					entry.m_Caster.PlaySound( 0x27 );

				entry.m_Timer.Stop();

				m_WeaponDamageTable.Remove( weapon );

				weapon.InvalidateProperties();
			}
		}

		private class ImmolatingWeaponEntry
		{
			public int m_Damage;
			public Timer m_Timer;
			public Mobile m_Caster;

			public ImmolatingWeaponEntry( int damage, Timer stopTimer, Mobile caster )
			{
				m_Damage = damage;
				m_Timer = stopTimer;
				m_Caster = caster;
			}
		}

		private class DelayedEffectEntry
		{
			public BaseWeapon m_Weapon;
			public Mobile m_Target;

			public DelayedEffectEntry( BaseWeapon weapon, Mobile target )
			{
				m_Weapon = weapon;
				m_Target = target;
			}
		}
	}
}
