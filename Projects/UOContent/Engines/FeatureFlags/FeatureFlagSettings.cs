using System;
using System.IO;
using System.Text.Json.Serialization;

namespace Server.Systems.FeatureFlags;

public sealed class FeatureFlag
{
    public string Key { get; init; }
    public string Description { get; init; }
    public bool Enabled { get; set; }
    public bool DefaultEnabled { get; init; }
    public string Category { get; init; }
    public DateTime LastModified { get; set; }
    public string LastModifiedBy { get; set; }
}

public sealed class GumpBlockEntry
{
    [JsonIgnore]
    public Type GumpType { get; set; }
    public string GumpTypeName { get; init; }
    public string Reason { get; init; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
}

public sealed class UseReqBlockEntry
{
    [JsonIgnore]
    public Type ItemType { get; set; }
    public string ItemTypeName { get; init; }
    public string Reason { get; init; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
}

public sealed class SkillBlockEntry
{
    [JsonIgnore]
    public SkillName Skill { get; set; }
    public string SkillName { get; init; }
    public string Reason { get; init; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
}

public sealed class SpellBlockEntry
{
    [JsonIgnore]
    public Type SpellType { get; set; }
    [JsonIgnore]
    public int SpellId { get; set; }
    public string SpellTypeName { get; init; }
    public string Reason { get; init; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
}

public sealed class ContainerBlockEntry
{
    [JsonIgnore]
    public Type ContainerType { get; set; }
    public string ContainerTypeName { get; init; }
    public string Reason { get; init; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
}

public static class FeatureFlagSettings
{
    public const string DefaultGumpBlockedMessage = "This feature is temporarily disabled.";
    public const string DefaultUseReqBlockedMessage = "This item cannot be used at this time.";
    public const string DefaultContainerBlockedMessage = "This container cannot be opened at this time.";

    public static AccessLevel RequiredAccessLevel { get; set; } = AccessLevel.Administrator;
    public static bool LogChanges { get; set; } = true;
    public static bool BroadcastChangesToStaff { get; set; } = true;

    public static string SavePath => Path.Combine(Core.BaseDirectory, "Configuration", "FeatureFlags");
}
