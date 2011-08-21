using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.SkillHandlers;

namespace Server.Spells.Ninjitsu
{
	public class DeathStrike : NinjaMove
	{
		public DeathStrike()
		{
		}

		public override int BaseMana { get { return 30; } }
		public override double RequiredSkill { get { return 85.0; } }

		public override TextDefinition AbilityMessage { get { return new TextDefinition( 1063091 ); } } // You prepare to hit your opponent with a Death Strike.

		public override double GetDamageScalar( Mobile attacker, Mobile defender )
		{
			return 0.5;
		}

		public override void OnHit( Mobile attacker, Mobile defender, int damage )
		{
			if( !Validate( attacker ) || !CheckMana( attacker, true ) )
				return;

			ClearCurrentMove( attacker );

			double ninjitsu = attacker.Skills[SkillName.Ninjitsu].Value;

			double chance;
			bool isRanged = false; // should be defined onHit method, what if the player hit and remove the weapon before process? ;)

			if ( attacker.Weapon is BaseRanged )
				isRanged = true;

			if( ninjitsu < 100 ) //This formula is an approximation from OSI data.  TODO: find correct formula
				chance = 30 + (ninjitsu - 85) * 2.2;
			else
				chance = 63 + (ninjitsu - 100) * 1.1;

			if( (chance / 100) < Utility.RandomDouble() )
			{
				attacker.SendLocalizedMessage( 1070779 ); // You missed your opponent with a Death Strike.
				return;
			}


			DeathStrikeInfo info;

			int damageBonus = 0;

			if( m_Table.Contains( defender ) )
			{
				defender.SendLocalizedMessage( 1063092 ); // Your opponent lands another Death Strike!

				info = (DeathStrikeInfo)m_Table[defender];

				if( info.m_Steps > 0 )
					damageBonus = attacker.Skills[SkillName.Ninjitsu].Fixed / 150;

				if( info.m_Timer != null )
					info.m_Timer.Stop();

				m_Table.Remove( defender );
			}
			else
			{
				defender.SendLocalizedMessage( 1063093 ); // You have been hit by a Death Strike!  Move with caution!
			}

			attacker.SendLocalizedMessage( 1063094 ); // You inflict a Death Strike upon your opponent!

			defender.FixedParticles( 0x374A, 1, 17, 0x26BC, EffectLayer.Waist );
			attacker.PlaySound( attacker.Female ? 0x50D : 0x50E );

			info = new DeathStrikeInfo( defender, attacker, damageBonus, isRanged );
			info.m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 5.0 ), new TimerStateCallback( ProcessDeathStrike ), defender );

			m_Table[defender] = info;

			CheckGain( attacker );
		}


		private static Hashtable m_Table = new Hashtable();

		private class DeathStrikeInfo
		{
			public Mobile m_Target;
			public Mobile m_Attacker;
			public int m_Steps;
			public int m_DamageBonus;
			public Timer m_Timer;
			public bool m_isRanged;

			public DeathStrikeInfo( Mobile target, Mobile attacker, int damageBonus, bool isRanged )
			{
				m_Target = target;
				m_Attacker = attacker;
				m_DamageBonus = damageBonus;
				m_isRanged = isRanged;
			}
		}

		public static void AddStep( Mobile m )
		{
			DeathStrikeInfo info = m_Table[m] as DeathStrikeInfo;

			if( info == null )
				return;

			if( ++info.m_Steps >= 5 )
				ProcessDeathStrike( m );
		}

		private static void ProcessDeathStrike( object state )
		{
			Mobile defender = (Mobile)state;

			DeathStrikeInfo info = m_Table[defender] as DeathStrikeInfo;

			if( info == null )	//sanity
				return;

			int maxDamage, damage = 0;

			double ninjitsu = info.m_Attacker.Skills[SkillName.Ninjitsu].Value;
			double stalkingBonus = Tracking.GetStalkingBonus( info.m_Attacker, info.m_Target );

			if ( Core.ML )
			{
				double scalar = ( info.m_Attacker.Skills[SkillName.Hiding].Value + info.m_Attacker.Skills[SkillName.Stealth].Value ) / 220;

				if ( scalar > 1 )
					scalar = 1;

				// New formula doesn't apply DamageBonus anymore, caps must be, directly, 60/30.
				if ( info.m_Steps >= 5 )
					damage = (int)Math.Floor( Math.Min( 60, ( ninjitsu / 3 ) * ( 0.3 + 0.7 * scalar ) + stalkingBonus ) );
				else
					damage = (int)Math.Floor( Math.Min( 30, ( ninjitsu / 9 ) * ( 0.3 + 0.7 * scalar ) + stalkingBonus ) );

				if ( info.m_isRanged )
					damage /= 2;
			}
			else
			{
				int divisor = (info.m_Steps >= 5) ? 30 : 80;
                        	double baseDamage = ninjitsu / divisor * 10;

				maxDamage = (info.m_Steps >= 5) ? 62 : 22; // DamageBonus is 8 at most. That brings the cap up to 70/30.
				damage = Math.Max( 0, Math.Min( maxDamage, (int)( baseDamage + stalkingBonus ) ) ) + info.m_DamageBonus;
			}

			if ( Core.ML )
				info.m_Target.Damage( damage, info.m_Attacker ); // Damage is direct.
			else
				AOS.Damage( info.m_Target, info.m_Attacker, damage, true, 100, 0, 0, 0, 0, 0, 0, false, false, true ); // Damage is physical.

			if( info.m_Timer != null )
				info.m_Timer.Stop();

			m_Table.Remove( info.m_Target );
		}
	}
}