using System;
using System.Collections.Generic;
using Server;

namespace Server.Engines.MLQuests
{
	public interface IQuestGiver
	{
		List<MLQuest> MLQuests { get; }

		Serial Serial { get; }
		bool Deleted { get; }

		Type GetType();
	}
}
