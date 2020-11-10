namespace Server
{
    public static class TestMapDefinitions
    {
        public static void ConfigureTestMapDefinitions()
        {
            RegisterMap(0, 0, 0, 7168, 4096, 4, "Felucca", MapRules.FeluccaRules);
            RegisterMap(1, 1, 1, 7168, 4096, 0, "Trammel", MapRules.TrammelRules);
            RegisterMap(2, 2, 2, 2304, 1600, 1, "Ilshenar", MapRules.TrammelRules);
            RegisterMap(3, 3, 3, 2560, 2048, 1, "Malas", MapRules.TrammelRules);
            RegisterMap(4, 4, 4, 1448, 1448, 1, "Tokuno", MapRules.TrammelRules);
            RegisterMap(5, 5, 5, 1280, 4096, 1, "TerMur", MapRules.TrammelRules);

            RegisterMap(0x7F, 0x7F, 0x7F, Map.SectorSize, Map.SectorSize, 1, "Internal", MapRules.Internal);
        }

        private static void RegisterMap(
            int mapIndex,
            int mapID,
            int fileIndex,
            int width,
            int height,
            int season,
            string name,
            MapRules rules
        )
        {
            var newMap = new Map(mapID, mapIndex, fileIndex, width, height, season, name, rules);

            Map.Maps[mapIndex] = newMap;
            Map.AllMaps.Add(newMap);
        }
    }
}
