namespace Server;

/// <summary>
/// Static boolean flags for Server project hot paths.
/// Values are synced by FeatureFlagManager in UOContent when flags change.
/// </summary>
public static class ServerFeatureFlags
{
    public static bool PlayerTrading { get; set; } = true;
    public static bool PvPCombat { get; set; } = true;
    public static bool BankAccess { get; set; } = true;
}
