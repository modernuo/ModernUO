using System;
using System.Linq;
using Server.Events.Halloween;
using Server.Items;

namespace Server.Engines.Events
{
    public static class PumpkinPatchSpawner
    {
        private static Timer m_Timer;

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

            if (now >= HolidaySettings.StartHalloween && now <= HolidaySettings.FinishHalloween)
            {
                m_Timer = Timer.DelayCall(TimeSpan.Zero, TimeSpan.FromSeconds(30), 0, PumpkinPatchSpawnerCallback);
            }
        }

        private static void PumpkinPatchSpawnerCallback()
        {
            AddPumpkin(Map.Felucca);
            AddPumpkin(Map.Trammel);
        }

        private static void AddPumpkin(Map map)
        {
            for (var i = 0; i < m_PumpkinFields.Length; i++)
            {
                var rect = m_PumpkinFields[i];

                var spawncount = rect.Height * rect.Width / 20;
                var pumpkins = map.GetItemsInBounds(rect).OfType<HalloweenPumpkin>().Count();

                if (spawncount > pumpkins)
                {
                    new HalloweenPumpkin().MoveToWorld(RandomPointIn(rect, map), map);
                }
            }
        }

        private static Point3D RandomPointIn(Rectangle2D rect, Map map)
        {
            var x = Utility.Random(rect.X, rect.Width);
            var y = Utility.Random(rect.Y, rect.Height);
            var z = map.GetAverageZ(x, y);

            return new Point3D(x, y, z);
        }
    }
}
