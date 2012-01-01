using System;
using System.Collections.Generic;
using Server;

namespace Server.Multis
{
	public class DynamicDecay
	{
		public static bool Enabled { get { return Core.ML; } }

		private static Dictionary<DecayLevel, DecayStageInfo> m_Stages;

		static DynamicDecay()
		{
			m_Stages = new Dictionary<DecayLevel, DecayStageInfo>();

			Register( DecayLevel.LikeNew,		TimeSpan.FromHours( 1 ),	TimeSpan.FromHours( 1 )		);
			Register( DecayLevel.Slightly,		TimeSpan.FromDays( 1 ),		TimeSpan.FromDays( 2 )		);
			Register( DecayLevel.Somewhat,		TimeSpan.FromDays( 1 ),		TimeSpan.FromDays( 2 )		);
			Register( DecayLevel.Fairly,		TimeSpan.FromDays( 1 ),		TimeSpan.FromDays( 2 )		);
			Register( DecayLevel.Greatly,		TimeSpan.FromDays( 1 ),		TimeSpan.FromDays( 2 )		);
			Register( DecayLevel.IDOC,			TimeSpan.FromHours( 12 ),	TimeSpan.FromHours( 24 )	);
		}

		public static void Register( DecayLevel level, TimeSpan min, TimeSpan max )
		{
			DecayStageInfo info = new DecayStageInfo( min, max );

			if ( m_Stages.ContainsKey( level ) )
				m_Stages[level] = info;
			else
				m_Stages.Add( level, info );
		}

		public static bool Decays( DecayLevel level )
		{
			return m_Stages.ContainsKey( level );
		}

		public static TimeSpan GetRandomDuration( DecayLevel level )
		{
			if ( !m_Stages.ContainsKey( level ) )
				return TimeSpan.Zero;

			DecayStageInfo info = m_Stages[level];
			long min = info.MinDuration.Ticks;
			long max = info.MaxDuration.Ticks;

			return TimeSpan.FromTicks( min + (long)( Utility.RandomDouble() * ( max - min ) ) );
		}
	}

	public class DecayStageInfo
	{
		private TimeSpan m_MinDuration;
		private TimeSpan m_MaxDuration;

		public TimeSpan MinDuration
		{
			get { return m_MinDuration; }
		}

		public TimeSpan MaxDuration
		{
			get { return m_MaxDuration; }
		}

		public DecayStageInfo( TimeSpan min, TimeSpan max )
		{
			m_MinDuration = min;
			m_MaxDuration = max;
		}
	}
}
