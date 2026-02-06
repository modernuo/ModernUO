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

public class FeatureFlagBlockEntry
{
    public Type ResolvedType { get; set; }
    public string Reason { get; init; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }

    [JsonIgnore]
    public virtual string DisplayName => ResolvedType?.Name;
}

public sealed class GumpBlockEntry : FeatureFlagBlockEntry;

public sealed class UseReqBlockEntry : FeatureFlagBlockEntry;

public sealed class SkillBlockEntry : FeatureFlagBlockEntry
{
    public SkillName Skill { get; set; }

    [JsonIgnore]
    public override string DisplayName => Skill.ToString();
}

public sealed class SpellBlockEntry : FeatureFlagBlockEntry
{
    [JsonIgnore]
    public int SpellId { get; set; }
}

public sealed class ContainerBlockEntry : FeatureFlagBlockEntry;

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
