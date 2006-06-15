using System;
using System.Collections;
using Server;
using Server.Items;

namespace Server.Engines.CannedEvil
{
	public class RestartTimer : Timer
	{
		private ChampionSpawn m_Spawn;

		public RestartTimer( ChampionSpawn spawn, TimeSpan delay ) : base( delay )
		{
			m_Spawn = spawn;
			Priority = TimerPriority.FiveSeconds;
		}

		protected override void OnTick()
		{
			m_Spawn.EndRestart();
		}
	}
}