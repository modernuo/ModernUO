namespace Server.Regions;

public static class RegionJsonDtoExt
{
    public static void Configure()
    {
        RegionJsonSerializer.Register<BaseRegionJsonDto<BaseRegion>, BaseRegion>();

        RegionJsonSerializer.Register<DungeonRegionJsonDto<DungeonRegion>, DungeonRegion>();
        RegionJsonSerializer.Register<GuardedRegionJsonDto<GuardedRegion>, GuardedRegion>();

        RegionJsonSerializer.Register<BaseRegionJsonDto<GreenAcresRegion>, GreenAcresRegion>();
        RegionJsonSerializer.Register<BaseRegionJsonDto<JailRegion>, JailRegion>();
        RegionJsonSerializer.Register<BaseRegionJsonDto<MondainRegion>, MondainRegion>();
        RegionJsonSerializer.Register<TownRegionJsonDto<NewMaginciaRegion>, NewMaginciaRegion>();
        RegionJsonSerializer.Register<BaseRegionJsonDto<NoHousingRegion>, NoHousingRegion>();
        RegionJsonSerializer.Register<BaseRegionJsonDto<NoHousingGuardedRegion>, NoHousingGuardedRegion>();
        RegionJsonSerializer.Register<BaseRegionJsonDto<NoTravelSpellsAllowedRegion>, NoTravelSpellsAllowedRegion>();
        RegionJsonSerializer.Register<TownRegionJsonDto<TownRegion>, TownRegion>();
        RegionJsonSerializer.Register<BaseRegionJsonDto<TwistedWealdDesertRegion>, TwistedWealdDesertRegion>();

        // Damage boost regions
        RegionJsonSerializer.Register<DungeonRegionJsonDto<CrystalFieldRegion>, CrystalFieldRegion>();
        RegionJsonSerializer.Register<DungeonRegionJsonDto<IcyRiverRegion>, IcyRiverRegion>();
        RegionJsonSerializer.Register<DungeonRegionJsonDto<AcidRiverRegion>, AcidRiverRegion>();
        RegionJsonSerializer.Register<DungeonRegionJsonDto<PoisonedTreeRegion>, PoisonedTreeRegion>();
        RegionJsonSerializer.Register<DungeonRegionJsonDto<LostCityEntranceRegion>, LostCityEntranceRegion>();
        RegionJsonSerializer.Register<DungeonRegionJsonDto<PoisonedCemeteryRegion>, PoisonedCemeteryRegion>();

        // Wrong Jail
        RegionJsonSerializer.Register<DungeonRegionJsonDto<WrongLevel3Region>, WrongLevel3Region>();
        RegionJsonSerializer.Register<DungeonRegionJsonDto<WrongJailRegion>, WrongJailRegion>();

        // Blackthorn Dungeon
        RegionJsonSerializer.Register<DungeonRegionJsonDto<BlackthornDungeonRegion>, BlackthornDungeonRegion>();

        RegionJsonSerializer.Register<GuardedRegionJsonDto<CousteauPerronHouseRegion>, CousteauPerronHouseRegion>();

        RegionJsonSerializer.Register<BaseRegionJsonDto<ApprenticeRegion>, ApprenticeRegion>();

        RegionJsonSerializer.Register<BaseRegionJsonDto<SeaMarketRegion>, SeaMarketRegion>();

        // Exploring the Deep
        RegionJsonSerializer.Register<DungeonRegionJsonDto<ExploringDeepCreaturesRegion>, ExploringDeepCreaturesRegion>();
        RegionJsonSerializer.Register<BaseRegionJsonDto<UnderwaterRegion>, UnderwaterRegion>();

        // Exodus Dungeon
        RegionJsonSerializer.Register<DungeonRegionJsonDto<ExodusDungeonRegion>, ExodusDungeonRegion>();

        // Doom
        RegionJsonSerializer.Register<DungeonRegionJsonDto<DoomGuardianRegion>, DoomGuardianRegion>();

        // Tokuno
        RegionJsonSerializer.Register<GuardedRegionJsonDto<TokunoDocksRegion>, TokunoDocksRegion>();

        // Tomb of Kings
        RegionJsonSerializer.Register<BaseRegionJsonDto<TombOfKingsRegion>, TombOfKingsRegion>();
        RegionJsonSerializer.Register<BaseRegionJsonDto<ToKBridgeRegion>, ToKBridgeRegion>();

        // Myrmidex
        RegionJsonSerializer.Register<DungeonRegionJsonDto<BattleRegion>, BattleRegion>();
    }
}
