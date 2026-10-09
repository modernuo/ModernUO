using Server.Mobiles;

namespace Server.Engines.Avatar;

/// <summary>
/// The safe zone where Avatars are reborn and the only place shop purchases are allowed.
/// Region name and respawn point are configuration so the starter island can be wired in without code changes.
/// </summary>
public static class AvatarSanctuary
{
    private static Map _respawnMap;

    public static string RegionName { get; private set; } = "Avatar Sanctuary";
    public static Point3D RespawnLocation { get; private set; } = new(5445, 1153, 0);
    public static Map RespawnMap => _respawnMap ?? Map.Felucca;

    public static void Configure()
    {
        RegionName = ServerConfiguration.GetOrUpdateSetting("avatar.sanctuary.region", "Avatar Sanctuary");

        var x = ServerConfiguration.GetOrUpdateSetting("avatar.sanctuary.x", 5445);
        var y = ServerConfiguration.GetOrUpdateSetting("avatar.sanctuary.y", 1153);
        var z = ServerConfiguration.GetOrUpdateSetting("avatar.sanctuary.z", 0);
        RespawnLocation = new Point3D(x, y, z);

        var mapName = ServerConfiguration.GetOrUpdateSetting("avatar.sanctuary.map", "Felucca");
        _respawnMap = Map.Parse(mapName);
    }

    public static bool IsIn(PlayerMobile from) => from.Region?.IsPartOf(RegionName) == true;

    public static void MoveTo(PlayerMobile from) => from.MoveToWorld(RespawnLocation, RespawnMap);
}
