using Server.Regions;

namespace Server.Engines.Doom
{
    public class GauntletRegion : BaseRegion
    {
        private GauntletSpawner m_Spawner;

        public GauntletRegion(GauntletSpawner spawner, Map map)
            : base(null, map, Find(spawner.Location, spawner.Map), spawner.RegionBounds)
        {
            m_Spawner = spawner;

            GoLocation = spawner.Location;

            Register();
        }

        public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
        {
            global = 12;
        }

        public override void OnEnter(Mobile m)
        {
        }

        public override void OnExit(Mobile m)
        {
        }
    }
}
