using System;
using Server.Items;
using Server.Utilities;

namespace Server.Engines.Plants
{
    public class PlantResourceInfo
    {
        private static readonly PlantResourceInfo[] m_ResourceList =
        {
            new(PlantType.ElephantEarPlant, PlantHue.BrightRed, typeof(RedLeaves)),
            new(PlantType.PonytailPalm, PlantHue.BrightRed, typeof(RedLeaves)),
            new(PlantType.CenturyPlant, PlantHue.BrightRed, typeof(RedLeaves)),
            new(PlantType.Poppies, PlantHue.BrightOrange, typeof(OrangePetals)),
            new(PlantType.Bulrushes, PlantHue.BrightOrange, typeof(OrangePetals)),
            new(PlantType.PampasGrass, PlantHue.BrightOrange, typeof(OrangePetals)),
            new(PlantType.SnakePlant, PlantHue.BrightGreen, typeof(GreenThorns)),
            new(PlantType.BarrelCactus, PlantHue.BrightGreen, typeof(GreenThorns)),
            new(PlantType.CocoaTree, PlantHue.Plain, typeof(CocoaPulp))
        };

        private PlantResourceInfo(PlantType plantType, PlantHue plantHue, Type resourceType)
        {
            PlantType = plantType;
            PlantHue = plantHue;
            ResourceType = resourceType;
        }

        public PlantType PlantType { get; }

        public PlantHue PlantHue { get; }

        public Type ResourceType { get; }

        public static PlantResourceInfo GetInfo(PlantType plantType, PlantHue plantHue)
        {
            foreach (var info in m_ResourceList)
            {
                if (info.PlantType == plantType && info.PlantHue == plantHue)
                {
                    return info;
                }
            }

            return null;
        }

        public Item CreateResource() => ResourceType.CreateInstance<Item>();
    }
}
