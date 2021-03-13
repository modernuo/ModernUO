using System;

namespace Server.Engines.Quests
{
    public class QuestRestartInfo
    {
        public QuestRestartInfo(Type questType, TimeSpan restartDelay)
        {
            QuestType = questType;
            Reset(restartDelay);
        }

        public QuestRestartInfo(Type questType, DateTime restartTime)
        {
            QuestType = questType;
            RestartTime = restartTime;
        }

        public Type QuestType { get; set; }

        public DateTime RestartTime { get; set; }

        public void Reset(TimeSpan restartDelay)
        {
            if (restartDelay < TimeSpan.MaxValue)
            {
                RestartTime = Core.Now + restartDelay;
            }
            else
            {
                RestartTime = DateTime.MaxValue;
            }
        }
    }
}
