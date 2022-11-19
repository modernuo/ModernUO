namespace Server.Regions;

public static class RegionJsonDtoExt
{
    public static void Configure()
    {
        RegionJsonSerializer.RegisterRegionForSerialization<BaseRegionJsonDto<BaseRegion>, BaseRegion>();

        RegionJsonSerializer.RegisterRegionForSerialization<DungeonRegionJsonDto<DungeonRegion>, DungeonRegion>();
        RegionJsonSerializer.RegisterRegionForSerialization<GuardedRegionJsonDto<GuardedRegion>, GuardedRegion>();

        RegionJsonSerializer.RegisterRegionForSerialization<BaseRegionJsonDto<GreenAcresRegion>, GreenAcresRegion>();
        RegionJsonSerializer.RegisterRegionForSerialization<BaseRegionJsonDto<JailRegion>, JailRegion>();
        RegionJsonSerializer.RegisterRegionForSerialization<BaseRegionJsonDto<MondainRegion>, MondainRegion>();
        RegionJsonSerializer.RegisterRegionForSerialization<BaseRegionJsonDto<NewMaginciaRegion>, NewMaginciaRegion>();
        RegionJsonSerializer.RegisterRegionForSerialization<BaseRegionJsonDto<NoHousingRegion>, NoHousingRegion>();
        RegionJsonSerializer.RegisterRegionForSerialization<BaseRegionJsonDto<NoTravelSpellsAllowedRegion>, NoTravelSpellsAllowedRegion>();
        RegionJsonSerializer.RegisterRegionForSerialization<BaseRegionJsonDto<TownRegion>, TownRegion>();
        RegionJsonSerializer.RegisterRegionForSerialization<BaseRegionJsonDto<TwistedWealdDesertRegion>, TwistedWealdDesertRegion>();
    }
}
