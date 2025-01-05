namespace Server.Items
{
    public enum SlayerName
    {
        None,
        Silver,
        OrcSlaying,
        TrollSlaughter,
        OgreTrashing,
        Repond,
        DragonSlaying,
        Terathan,
        SnakesBane,
        LizardmanSlaughter,
        ReptilianDeath,
        DaemonDismissal,
        GargoylesFoe,
        BalronDamnation,
        Exorcism,
        Ophidian,
        SpidersDeath,
        ScorpionsBane,
        ArachnidDoom,
        FlameDousing,
        WaterDissipation,
        Vacuum,
        ElementalHealth,
        EarthShatter,
        BloodDrinking,
        SummerWind,
        ElementalBan, // Bane?
        Fey
    }

    public static class SlayerNameExtensions
    {
        public static string GetSlayerNamePreAOS(this SlayerName slayerName, Mobile from)
        {
            if (slayerName == SlayerName.None)
            {
                return "none";
            }

            return slayerName switch
            {
                SlayerName.Silver => Localization.GetText(1017384, from.Language).ToLowerInvariant(),
                SlayerName.OrcSlaying => Localization.GetText(1017385, from.Language).ToLowerInvariant(),
                SlayerName.TrollSlaughter => Localization.GetText(1017386, from.Language).ToLowerInvariant(),
                SlayerName.OgreTrashing => Localization.GetText(1017387, from.Language).ToLowerInvariant(),
                SlayerName.Repond => Localization.GetText(1017388, from.Language).ToLowerInvariant(),
                SlayerName.DragonSlaying => Localization.GetText(1017389, from.Language).ToLowerInvariant(),
                SlayerName.Terathan => Localization.GetText(1017390, from.Language).ToLowerInvariant(),
                SlayerName.SnakesBane => Localization.GetText(1017391, from.Language).ToLowerInvariant(),
                SlayerName.LizardmanSlaughter => Localization.GetText(1017392, from.Language).ToLowerInvariant(),
                SlayerName.ReptilianDeath => Localization.GetText(1017393, from.Language).ToLowerInvariant(),
                SlayerName.DaemonDismissal => Localization.GetText(1017394, from.Language).ToLowerInvariant(),
                SlayerName.GargoylesFoe => Localization.GetText(1017395, from.Language).ToLowerInvariant(),
                SlayerName.BalronDamnation => Localization.GetText(1017396, from.Language).ToLowerInvariant(),
                SlayerName.Exorcism => Localization.GetText(1017397, from.Language).ToLowerInvariant(),
                SlayerName.Ophidian => Localization.GetText(1017398, from.Language).ToLowerInvariant(),
                SlayerName.SpidersDeath => Localization.GetText(1017399, from.Language).ToLowerInvariant(),
                SlayerName.ScorpionsBane => Localization.GetText(1017400, from.Language).ToLowerInvariant(),
                SlayerName.ArachnidDoom => Localization.GetText(1017401, from.Language).ToLowerInvariant(),
                SlayerName.FlameDousing => Localization.GetText(1017402, from.Language).ToLowerInvariant(),
                SlayerName.WaterDissipation => Localization.GetText(1017403, from.Language).ToLowerInvariant(),
                SlayerName.Vacuum => Localization.GetText(1017404, from.Language).ToLowerInvariant(),
                SlayerName.ElementalHealth => Localization.GetText(1017405, from.Language).ToLowerInvariant(),
                SlayerName.EarthShatter => Localization.GetText(1017406, from.Language).ToLowerInvariant(),
                SlayerName.BloodDrinking => Localization.GetText(1017407, from.Language).ToLowerInvariant(),
                SlayerName.SummerWind => Localization.GetText(1017408, from.Language).ToLowerInvariant(),
                SlayerName.ElementalBan => Localization.GetText(1017409, from.Language).ToLowerInvariant(),
                SlayerName.Fey => Localization.GetText(1070855, from.Language).ToLowerInvariant(),
                _ => "unknown slayer"
            };
        }
    }
}
