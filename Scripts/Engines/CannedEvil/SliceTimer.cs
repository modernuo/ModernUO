using System;
using System.Collections;
using Server;
using Server.Items;

namespace Server.Engines.CannedEvil
{
	public class SliceTimer : Timer
	{
		private ChampionSpawn m_Spawn;

		public SliceTimer( ChampionSpawn spawn ) : base( TimeSpan.FromSeconds( 1.0 ),  TimeSpan.FromSeconds( 1.0 ) )
		{
			m_Spawn = spawn;
			Priority = TimerPriority.OneSecond;
		}

		protected override void OnTick()
		{
			m_Spawn.OnSlice();
		}
	}
}