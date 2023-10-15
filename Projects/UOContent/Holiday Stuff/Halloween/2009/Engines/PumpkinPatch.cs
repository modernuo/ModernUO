using System;
using Server.Events.Halloween;
using Server.Items;

namespace Server.Engines.Events
{
    public static class PumpkinPatchSpawner
    {
        private static Timer _timer;

        private static readonly Rectangle2D[] m_PumpkinFields =
        {
            new(4557, 1471, 20, 10),
            new(796, 2152, 36, 24),
            new(816, 2251, 16, 8),
            new(816, 2261, 16, 8),
            new(816, 2271, 16, 8),
            new(816, 2281, 16, 8),
            new(835, 2344, 16, 16),
            new(816, 2344, 16, 24)
        };

        public static void Initialize()
        {
            var now = Core.Now;

            // TODO: World timer to turn these on/off
            if (now >= HolidaySettings.StartHalloween && now <= HolidaySettings.FinishHalloween)
            {
                _timer = Timer.DelayCall(TimeSpan.FromSeconds(30), 0, PumpkinPatchSpawnerCallback);
            }
        }

        private static void PumpkinPatchSpawnerCallback()
        {
            AddPumpkin(Map.Felucca);
            AddPumpkin(Map.Trammel);

            if (Core.Now > HolidaySettings.FinishHalloween)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        private static void AddPumpkin(Map map)
        {
            for (var i = 0; i < m_PumpkinFields.Length; i++)
            {
                var rect = m_PumpkinFields[i];

                var spawncount = rect.Height * rect.Width / 20;
                var pumpkins = 0;
                foreach (var _ in map.GetItemsInBounds<HalloweenPumpkin>(rect))
                {
                    if (pumpkins++ >= spawncount)
                    {
                        break;
                    }
                }

                if (spawncount > pumpkins)
                {
                    new HalloweenPumpkin().MoveToWorld(Utility.RandomPointIn(rect, map), map);
                }
            }
        }
    }
}
