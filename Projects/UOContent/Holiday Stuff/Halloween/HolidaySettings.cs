using System;
using Server.Items;
using Server.Utilities;

namespace Server.Events.Halloween
{
    internal class HolidaySettings
    {
        private static readonly Type[] m_GMBeggarTreats =
        {
            typeof(CreepyCake),
            typeof(PumpkinPizza),
            typeof(GrimWarning),
            typeof(HarvestWine),
            typeof(MurkyMilk),
            typeof(MrPlainsCookies),
            typeof(SkullsOnPike),
            typeof(ChairInAGhostCostume),
            typeof(ExcellentIronMaiden),
            typeof(HalloweenGuillotine),
            typeof(ColoredSmallWebs)
        };

        private static readonly Type[] m_Treats =
        {
            typeof(Lollipops),
            typeof(WrappedCandy),
            typeof(JellyBeans),
            typeof(Taffy),
            typeof(NougatSwirl)
        };

        public static DateTime StartHalloween // YY MM DD
            => new(2012, 10, 24);

        public static DateTime FinishHalloween => new(2012, 11, 15);

        public static Item RandomGMBeggerItem =>
            m_GMBeggarTreats.RandomElement().CreateInstance<Item>();

        public static Item RandomTreat => m_Treats.RandomElement().CreateInstance<Item>();
    }
}
