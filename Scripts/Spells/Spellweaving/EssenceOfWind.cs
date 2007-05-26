using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Spells.Spellweaving
{
	public class EssenceOfWindSpell : ArcanistSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
                "Essence of Wind", "Anathrae",
				-1
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 3.0 ); } }

        public override double RequiredSkill { get { return 52.0; } }
        public override int RequiredMana { get { return 40; } }

        public EssenceOfWindSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void OnCast()
		{
			if( CheckSequence() )
			{
				Caster.PlaySound( 0x5C6 );

				int range = 5 + FocusLevel;
				int damage = 25 + FocusLevel;

				double skill = Caster.Skills[SkillName.Spellweaving].Value;

				TimeSpan duration = TimeSpan.FromSeconds( (int)(skill / 24) + FocusLevel );

				int fcMalus = 2 * (FocusLevel + 1);
				int ssiMalus = FocusLevel + 1;

				List<Mobile> targets = new List<Mobile>();

				foreach( Mobile m in Caster.GetMobilesInRange( range ) )
				{
					if( Caster != m && Caster.InLOS( m ) && SpellHelper.ValidIndirectTarget( Caster, m ) && Caster.CanBeHarmful( m, false ) )
						targets.Add( m );
				}

				for( int i = 0; i < targets.Count; i++ )
				{
					Mobile m = targets[i];

					Caster.DoHarmful( m );

					SpellHelper.Damage( this, m, damage, 0, 0, 100, 0, 0 );

					if( !CheckResisted( m ) )	//No message on resist
					{
						m_Table[m] = new EssenceOfWindInfo( m, fcMalus, ssiMalus, duration );

						BuffInfo.AddBuff( m, new BuffInfo( BuffIcon.EssenceOfWind, 1075802, duration, m, String.Format( "{0}\t{1}", fcMalus.ToString(), ssiMalus.ToString() ) ) );
					}
				}
			}

			FinishSequence();
		}

		private static Dictionary<Mobile, EssenceOfWindInfo> m_Table = new Dictionary<Mobile, EssenceOfWindInfo>();

		private class EssenceOfWindInfo
		{
			private Mobile m_Defender;
			private int m_FCMalus;
			private int m_SSIMalus;
			private ExpireTimer m_Timer;

			public Mobile Defender { get { return m_Defender; } }
			public int FCMalus { get { return m_FCMalus; } }
			public int SSIMalus { get { return m_SSIMalus; } }
			public ExpireTimer Timer { get { return m_Timer; } }

			public EssenceOfWindInfo( Mobile defender, int fcMalus, int ssiMalus, TimeSpan duration )
			{
				m_Defender = defender;
				m_FCMalus = fcMalus;
				m_SSIMalus = ssiMalus;

				m_Timer = new ExpireTimer( m_Defender, duration );
				m_Timer.Start();
			}
		}

		public static int GetFCMalus( Mobile m )
		{
			EssenceOfWindInfo info;

			if( m_Table.TryGetValue( m, out info ) )
				return info.FCMalus;

			return 0;
		}

		public static int GetSSIMalus( Mobile m )
		{
			EssenceOfWindInfo info;

			if( m_Table.TryGetValue( m, out info ) )
				return info.SSIMalus;

			return 0;
		}

		public static bool IsDebuffed( Mobile m )
		{
			return m_Table.ContainsKey( m );
		}

		public static void StopDebuffing( Mobile m, bool message )
		{
			EssenceOfWindInfo info;

			if( m_Table.TryGetValue( m, out info ) )
				info.Timer.DoExpire( message );
		}

		private class ExpireTimer : Timer
		{
			private Mobile m_Mobile;

			public ExpireTimer( Mobile m, TimeSpan delay ) : base( delay )
			{
				m_Mobile = m;
			}

			protected override void OnTick()
			{
				DoExpire( true );
			}

			public void DoExpire( bool message )
			{
				Stop();
				/*
				if( message )
				{
				}
				*/
				m_Table.Remove( m_Mobile );

				BuffInfo.RemoveBuff( m_Mobile, BuffIcon.EssenceOfWind );
			}
		}
	}
}