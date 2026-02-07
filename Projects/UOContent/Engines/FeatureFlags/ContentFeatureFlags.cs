namespace Server.Systems.FeatureFlags;

/// <summary>
/// Static boolean flags for UOContent hot paths.
/// Values are synced by FeatureFlagManager when flags change.
/// </summary>
public static class ContentFeatureFlags
{
    public static bool VendorPurchase { get; set; } = true;
    public static bool VendorSell { get; set; } = true;
    public static bool PlayerVendors { get; set; } = true;
    public static bool HousePlacement { get; set; } = true;
    public static bool BoatPlacement { get; set; } = true;
    public static bool BulkOrders { get; set; } = true;
}
