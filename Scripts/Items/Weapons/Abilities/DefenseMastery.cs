using System;
using Server;
using System.Collections;

namespace Server.Items
{
	/// <summary>
	/// Raises your physical resistance for a short time while lowering your ability to inflict damage. Requires Bushido or Ninjitsu skill.
	/// </summary>
	public class DefenseMastery : WeaponAbility
	{
		public DefenseMastery()
		{
		}

		public override bool CheckSkills( Mobile from )
		{
			if( GetSkill( from, SkillName.Ninjitsu ) < 50.0  && GetSkill( from, SkillName.Bushido ) < 50.0 )
			{
				from.SendLocalizedMessage( 1063347, "50" ); // You need ~1_SKILL_REQUIREMENT~ Bushido or Ninjitsu skill to perform that attack!
				return false;
			}

			return base.CheckSkills( from );
		}

		public override int BaseMana { get { return 30; } }

		public override void OnHit( Mobile attacker, Mobile defender, int damage )
		{
			if( !Validate( attacker ) || !CheckMana( attacker, true ) )
				return;

			ClearCurrentAbility( attacker );

			attacker.SendLocalizedMessage( 1063353 ); // You perform a masterful defense!

			attacker.FixedParticles( 0x375A, 1, 17, 0x7F2, 0x3E8, 0x3, EffectLayer.Waist );

			int modifier = (int)(30.0 * ((Math.Max( attacker.Skills[SkillName.Bushido].Value, attacker.Skills[SkillName.Ninjitsu].Value ) - 50.0) / 70.0));

			DefenseMasteryInfo info = m_Table[attacker] as DefenseMasteryInfo;

			if( info != null )
				EndDefense( (object)info );

			ResistanceMod mod = new ResistanceMod( ResistanceType.Physical, 50 + modifier );
			attacker.AddResistanceMod( mod );

			info = new DefenseMasteryInfo( attacker, 80 - modifier, mod );
			info.m_Timer = Timer.DelayCall( TimeSpan.FromSeconds( 3.0 ), new TimerStateCallback( EndDefense ), info );

			m_Table[attacker] = info;

			attacker.Delta( MobileDelta.WeaponDamage );
		}

		private class DefenseMasteryInfo
		{
			public Mobile m_From;
			public Timer m_Timer;
			public int m_DamageMalus;
			public ResistanceMod m_Mod;

			public DefenseMasteryInfo( Mobile from, int damageMalus, ResistanceMod mod )
			{
				m_From = from;
				m_DamageMalus = damageMalus;
				m_Mod = mod;
			}
		}

		private static Hashtable m_Table = new Hashtable();

		public static bool GetMalus( Mobile targ, ref int damageMalus )
		{
			DefenseMasteryInfo info = m_Table[targ] as DefenseMasteryInfo;

			if( info == null )
				return false;

			damageMalus = info.m_DamageMalus;
			return true;
		}

		private static void EndDefense( object state )
		{
			DefenseMasteryInfo info = (DefenseMasteryInfo)state;

			if( info.m_Mod != null )
				info.m_From.RemoveResistanceMod( info.m_Mod );

			if( info.m_Timer != null )
				info.m_Timer.Stop();

			// No message is sent to the player.

			m_Table.Remove( info.m_From );

			info.m_From.Delta( MobileDelta.WeaponDamage );
		}
	}
}