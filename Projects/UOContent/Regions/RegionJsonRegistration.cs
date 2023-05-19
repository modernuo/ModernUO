namespace Server.Regions;

public static class RegionJsonRegistration
{
    public static void Configure()
    {
        RegionJsonSerializer.Register<BaseRegion>();
        RegionJsonSerializer.Register<TownRegion>();
        RegionJsonSerializer.Register<DungeonRegion>();
        RegionJsonSerializer.Register<GuardedRegion>();

        // Travel registricted regions
        RegionJsonSerializer.Register<GreenAcresRegion>();
        RegionJsonSerializer.Register<JailRegion>();
        RegionJsonSerializer.Register<MondainRegion>();
        RegionJsonSerializer.Register<NewMaginciaRegion>();
        RegionJsonSerializer.Register<NoHousingRegion>();
        RegionJsonSerializer.Register<NoHousingGuardedRegion>();
        RegionJsonSerializer.Register<NoTravelSpellsAllowedRegion>();
        RegionJsonSerializer.Register<TwistedWealdDesertRegion>();

        // Damage boost regions
        RegionJsonSerializer.Register<CrystalFieldRegion>();
        RegionJsonSerializer.Register<IcyRiverRegion>();
        RegionJsonSerializer.Register<AcidRiverRegion>();
        RegionJsonSerializer.Register<PoisonedTreeRegion>();
        RegionJsonSerializer.Register<LostCityEntranceRegion>();
        RegionJsonSerializer.Register<PoisonedCemeteryRegion>();

        // Wrong Jail
        RegionJsonSerializer.Register<WrongLevel3Region>();
        RegionJsonSerializer.Register<WrongJailRegion>();

        // Blackthorn Dungeon
        RegionJsonSerializer.Register<BlackthornDungeonRegion>();

        RegionJsonSerializer.Register<CousteauPerronHouseRegion>();

        RegionJsonSerializer.Register<ApprenticeRegion>();

        RegionJsonSerializer.Register<SeaMarketRegion>();

        // Exploring the Deep
        RegionJsonSerializer.Register<ExploringDeepCreaturesRegion>();
        RegionJsonSerializer.Register<UnderwaterRegion>();

        // Exodus Dungeon
        RegionJsonSerializer.Register<ExodusDungeonRegion>();

        // Doom
        RegionJsonSerializer.Register<DoomGuardianRegion>();

        // Tokuno
        RegionJsonSerializer.Register<TokunoDocksRegion>();

        // Tomb of Kings
        RegionJsonSerializer.Register<TombOfKingsRegion>();
        RegionJsonSerializer.Register<ToKBridgeRegion>();

        // Myrmidex
        RegionJsonSerializer.Register<BattleRegion>();
    }
}
