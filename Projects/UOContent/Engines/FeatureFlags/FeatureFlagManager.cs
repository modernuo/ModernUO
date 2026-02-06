using System;
using System.Collections.Generic;
using System.IO;
using Server.Gumps;
using Server.Items;
using Server.Json;
using Server.Logging;
using Server.Network;
using Server.Spells;

namespace Server.Systems.FeatureFlags;

public static class FeatureFlagManager
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FeatureFlagManager));

    // Primary storage
    private static readonly Dictionary<string, FeatureFlag> _flags = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<Type, GumpBlockEntry> _gumpBlocks = new();
    private static readonly Dictionary<Type, UseReqBlockEntry> _useReqBlocks = new();
    private static readonly SpellBlockEntry[] _spellBlocks = new SpellBlockEntry[SpellRegistry.Types.Length];
    private static readonly Dictionary<Type, ContainerBlockEntry> _containerBlocks = new();
    private static readonly SkillBlockEntry[] _skillBlocks = new SkillBlockEntry[58];

    // Fast bailout flags
    private static bool _hasActiveGumpBlocks;
    private static bool _hasActiveUseReqBlocks;
    private static bool _hasActiveSkillBlocks;
    private static bool _hasActiveSpellBlocks;
    private static bool _hasActiveContainerBlocks;

    private static bool _initialized;

    public static unsafe void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        var savePath = FeatureFlagSettings.SavePath;
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // Load predefined flags from JSON, then overlay runtime state
        LoadDefaultFlags();
        Load();

        // Wire container display access check
        Container.DisplayAccessCheck = &CheckContainerAccess;

        _initialized = true;
        logger.Information(
            "Feature Flag system initialized with {FlagCount} flags, {GumpBlockCount} gump blocks, {UseReqBlockCount} UseReq blocks, {SkillBlockCount} skill blocks, {SpellBlockCount} spell blocks, {ContainerBlockCount} container blocks",
            _flags.Count, _gumpBlocks.Count, _useReqBlocks.Count, CountActiveSkillBlocks(), CountActiveSpellBlocks(), _containerBlocks.Count);
    }

    private static int CountActiveSkillBlocks()
    {
        var count = 0;
        for (var i = 0; i < _skillBlocks.Length; i++)
        {
            if (_skillBlocks[i] != null)
            {
                count++;
            }
        }
        return count;
    }

    private static int CountActiveSpellBlocks()
    {
        var count = 0;
        for (var i = 0; i < _spellBlocks.Length; i++)
        {
            if (_spellBlocks[i] != null)
            {
                count++;
            }
        }
        return count;
    }

    public static bool IsEnabled(string flagKey) => _flags.TryGetValue(flagKey, out var flag) && flag.Enabled;

    public static FeatureFlag GetFlag(string flagKey) => _flags.GetValueOrDefault(flagKey);

    public static IReadOnlyCollection<FeatureFlag> GetAllFlags() => _flags.Values;

    public static bool SetFlag(string flagKey, bool enabled, string modifiedBy = "System")
    {
        if (!_flags.TryGetValue(flagKey, out var flag))
        {
            return false;
        }

        var previousState = flag.Enabled;
        flag.Enabled = enabled;
        flag.LastModified = Core.Now;
        flag.LastModifiedBy = modifiedBy;

        SyncStaticFlag(flagKey, enabled);

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Information("Feature flag '{FlagKey}' changed from {Previous} to {Current} by {ModifiedBy}",
                flagKey, previousState, enabled, modifiedBy);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Feature Flag] '{flagKey}' {(enabled ? "ENABLED" : "DISABLED")} by {modifiedBy}");
        }

        SaveFlags();
        return true;
    }

    public static FeatureFlag CreateOrUpdateFlag(string key, string description, string category, bool defaultEnabled, string createdBy = "System")
    {
        var flag = new FeatureFlag
        {
            Key = key,
            Description = description,
            Category = category,
            DefaultEnabled = defaultEnabled,
            Enabled = defaultEnabled,
            LastModified = Core.Now,
            LastModifiedBy = createdBy
        };

        _flags[key] = flag;

        SyncStaticFlag(key, flag.Enabled);

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Information("Feature flag '{FlagKey}' created/updated by {CreatedBy}", key, createdBy);
        }

        SaveFlags();
        return flag;
    }

    public static bool RemoveFlag(string flagKey, string removedBy = "System")
    {
        if (_flags.Remove(flagKey))
        {
            SyncStaticFlag(flagKey, true); // Reset to default enabled

            if (FeatureFlagSettings.LogChanges)
            {
                logger.Information("Feature flag '{FlagKey}' removed by {RemovedBy}", flagKey, removedBy);
            }

            SaveFlags();
            return true;
        }
        return false;
    }

    public static bool IsGumpBlocked<T>() where T : BaseGump
    {
        if (!_hasActiveGumpBlocks)
        {
            return false;
        }

        return _gumpBlocks.TryGetValue(typeof(T), out var entry) && entry.Active;
    }

    public static bool IsGumpBlocked(Type gumpType)
    {
        if (!_hasActiveGumpBlocks)
        {
            return false;
        }

        return _gumpBlocks.TryGetValue(gumpType, out var entry) && entry.Active;
    }

    public static GumpBlockEntry GetGumpBlockEntry(Type gumpType)
    {
        if (!_hasActiveGumpBlocks)
        {
            return null;
        }

        return _gumpBlocks.TryGetValue(gumpType, out var entry) ? entry : null;
    }

    public static IReadOnlyCollection<GumpBlockEntry> GetAllGumpBlocks() => _gumpBlocks.Values;

    public static void BlockGump<T>(string reason, string blockedBy = "System") where T : BaseGump
    {
        BlockGump(typeof(T), reason, blockedBy);
    }

    public static void BlockGump(Type gumpType, string reason, string blockedBy = "System")
    {
        var typeName = gumpType.FullName ?? gumpType.Name;
        var entry = new GumpBlockEntry
        {
            GumpType = gumpType,
            GumpTypeName = typeName,
            Reason = reason,
            Active = true,
            CreatedAt = Core.Now,
            CreatedBy = blockedBy
        };

        _gumpBlocks[gumpType] = entry;
        UpdateGumpBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Warning("Gump '{GumpType}' BLOCKED by {BlockedBy}. Reason: {Reason}", typeName, blockedBy, reason);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Gump Block] '{gumpType.Name}' BLOCKED by {blockedBy}. Reason: {reason}");
        }

        SaveGumpBlocks();
    }

    public static bool BlockGumpByName(string typeName, string reason, string blockedBy = "System")
    {
        var type = ResolveType(typeName);
        if (type == null || !typeof(BaseGump).IsAssignableFrom(type))
        {
            return false;
        }

        BlockGump(type, reason, blockedBy);
        return true;
    }

    public static bool UnblockGump<T>(string unblockedBy = "System") where T : BaseGump =>
        UnblockGump(typeof(T), unblockedBy);

    public static bool UnblockGump(Type gumpType, string unblockedBy = "System")
    {
        if (!_gumpBlocks.Remove(gumpType, out var removed))
        {
            return false;
        }

        UpdateGumpBlocksFlag();

        var typeName = removed.GumpTypeName;

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Information("Gump '{GumpType}' UNBLOCKED by {UnblockedBy}", typeName, unblockedBy);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Gump Block] '{gumpType.Name}' UNBLOCKED by {unblockedBy}");
        }

        SaveGumpBlocks();
        return true;
    }

    public static bool UnblockGumpByName(string typeName, string unblockedBy = "System")
    {
        var type = ResolveType(typeName);
        return type != null && UnblockGump(type, unblockedBy);
    }

    public static bool SetGumpBlockActive(string typeName, bool active, string modifiedBy = "System")
    {
        var type = ResolveType(typeName);
        if (type != null && _gumpBlocks.TryGetValue(type, out var entry))
        {
            entry.Active = active;
            UpdateGumpBlocksFlag();

            if (FeatureFlagSettings.LogChanges)
            {
                logger.Information("Gump block '{GumpType}' set to {Active} by {ModifiedBy}", typeName, active ? "ACTIVE" : "INACTIVE", modifiedBy);
            }

            if (FeatureFlagSettings.BroadcastChangesToStaff)
            {
                World.BroadcastStaff($"[Gump Block] '{typeName}' set to {(active ? "ACTIVE" : "INACTIVE")} by {modifiedBy}");
            }

            SaveGumpBlocks();
            return true;
        }

        return false;
    }

    public static bool IsUseReqBlocked<T>() where T : Item =>
        _hasActiveUseReqBlocks && _useReqBlocks.TryGetValue(typeof(T), out var entry) && entry.Active;

    public static bool IsUseReqBlocked(Type itemType) =>
        _hasActiveUseReqBlocks && _useReqBlocks.TryGetValue(itemType, out var entry) && entry.Active;

    public static bool IsUseReqBlocked(Item item) => item != null && IsUseReqBlocked(item.GetType());

    public static UseReqBlockEntry GetUseReqBlockEntry(Type itemType) =>
        _hasActiveUseReqBlocks ? _useReqBlocks.GetValueOrDefault(itemType) : null;

    public static UseReqBlockEntry GetUseReqBlockEntry(Item item) =>
        item != null ? GetUseReqBlockEntry(item.GetType()) : null;

    public static IReadOnlyCollection<UseReqBlockEntry> GetAllUseReqBlocks() => _useReqBlocks.Values;

    public static void BlockUseReq<T>(string reason, string blockedBy = "System") where T : Item =>
        BlockUseReq(typeof(T), reason, blockedBy);

    public static void BlockUseReq(Type itemType, string reason, string blockedBy = "System")
    {
        var typeName = itemType.FullName ?? itemType.Name;
        var entry = new UseReqBlockEntry
        {
            ItemType = itemType,
            ItemTypeName = typeName,
            Reason = reason,
            Active = true,
            CreatedAt = Core.Now,
            CreatedBy = blockedBy
        };

        _useReqBlocks[itemType] = entry;
        UpdateUseReqBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Warning("Item UseReq '{ItemType}' BLOCKED by {BlockedBy}. Reason: {Reason}", typeName, blockedBy, reason);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[UseReq Block] '{itemType.Name}' BLOCKED by {blockedBy}. Reason: {reason}");
        }

        SaveUseReqBlocks();
    }

    public static bool BlockUseReqByName(string typeName, string reason, string blockedBy = "System")
    {
        var type = ResolveType(typeName);
        if (type == null || !typeof(Item).IsAssignableFrom(type))
        {
            return false;
        }

        BlockUseReq(type, reason, blockedBy);
        return true;
    }

    public static bool UnblockUseReq<T>(string unblockedBy = "System") where T : Item =>
        UnblockUseReq(typeof(T), unblockedBy);

    public static bool UnblockUseReq(Type itemType, string unblockedBy = "System")
    {
        if (!_useReqBlocks.Remove(itemType, out var removed))
        {
            return false;
        }

        UpdateUseReqBlocksFlag();

        var typeName = removed.ItemTypeName;

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Information("Item UseReq '{ItemType}' UNBLOCKED by {UnblockedBy}", typeName, unblockedBy);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[UseReq Block] '{itemType.Name}' UNBLOCKED by {unblockedBy}");
        }

        SaveUseReqBlocks();
        return true;
    }

    public static bool UnblockUseReqByName(string typeName, string unblockedBy = "System")
    {
        var type = ResolveType(typeName);
        return type != null && UnblockUseReq(type, unblockedBy);
    }

    public static bool SetUseReqBlockActive(string typeName, bool active, string modifiedBy = "System")
    {
        var type  = ResolveType(typeName);
        if (type != null && _useReqBlocks.TryGetValue(type, out var entry))
        {
            entry.Active = active;
            UpdateUseReqBlocksFlag();

            if (FeatureFlagSettings.LogChanges)
            {
                logger.Information("UseReq block '{ItemType}' set to {Active} by {ModifiedBy}", typeName, active ? "ACTIVE" : "INACTIVE", modifiedBy);
            }

            if (FeatureFlagSettings.BroadcastChangesToStaff)
            {
                World.BroadcastStaff($"[UseReq Block] '{typeName}' set to {(active ? "ACTIVE" : "INACTIVE")} by {modifiedBy}");
            }

            SaveUseReqBlocks();
            return true;
        }

        return false;
    }

    public static string CheckUseReq(Item item, Mobile from, bool sendMessage = true)
    {
        if (!_hasActiveUseReqBlocks || item == null || from == null)
        {
            return null;
        }

        if (from.AccessLevel >= FeatureFlagSettings.RequiredAccessLevel)
        {
            return null;
        }

        if (_useReqBlocks.TryGetValue(item.GetType(), out var entry) && entry.Active)
        {
            var reason = entry.Reason ?? FeatureFlagSettings.DefaultUseReqBlockedMessage;
            if (sendMessage)
            {
                from.SendMessage(0x22, reason);
            }
            return reason;
        }

        return null;
    }

    public static bool IsSkillBlocked(SkillName skill)
    {
        if (!_hasActiveSkillBlocks)
        {
            return false;
        }

        var index = (int)skill;
        if ((uint)index >= (uint)_skillBlocks.Length)
        {
            return false;
        }

        var entry = _skillBlocks[index];
        return entry is { Active: true };
    }

    public static SkillBlockEntry GetSkillBlockEntry(SkillName skill)
    {
        var index = (int)skill;
        return (uint)index >= (uint)_skillBlocks.Length ? null : _skillBlocks[index];
    }

    public static IReadOnlyList<SkillBlockEntry> GetAllSkillBlocks()
    {
        var list = new List<SkillBlockEntry>();
        for (var i = 0; i < _skillBlocks.Length; i++)
        {
            if (_skillBlocks[i] != null)
            {
                list.Add(_skillBlocks[i]);
            }
        }
        return list;
    }

    public static void BlockSkill(SkillName skill, string reason, string blockedBy = "System")
    {
        var index = (int)skill;
        if ((uint)index >= (uint)_skillBlocks.Length)
        {
            return;
        }

        var entry = new SkillBlockEntry
        {
            Skill = skill,
            SkillName = skill.ToString(),
            Reason = reason,
            Active = true,
            CreatedAt = Core.Now,
            CreatedBy = blockedBy
        };

        _skillBlocks[index] = entry;
        UpdateSkillBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Warning("Skill '{Skill}' BLOCKED by {BlockedBy}. Reason: {Reason}", skill, blockedBy, reason);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Skill Block] '{skill}' BLOCKED by {blockedBy}. Reason: {reason}");
        }

        SaveSkillBlocks();
    }

    public static bool UnblockSkill(SkillName skill, string unblockedBy = "System")
    {
        var index = (int)skill;
        if ((uint)index >= (uint)_skillBlocks.Length)
        {
            return false;
        }

        if (_skillBlocks[index] != null)
        {
            _skillBlocks[index] = null;
            UpdateSkillBlocksFlag();

            if (FeatureFlagSettings.LogChanges)
            {
                logger.Information("Skill '{Skill}' UNBLOCKED by {UnblockedBy}", skill, unblockedBy);
            }

            if (FeatureFlagSettings.BroadcastChangesToStaff)
            {
                World.BroadcastStaff($"[Skill Block] '{skill}' UNBLOCKED by {unblockedBy}");
            }

            SaveSkillBlocks();
            return true;
        }

        return false;
    }

    public static bool SetSkillBlockActive(SkillName skill, bool active, string modifiedBy = "System")
    {
        var index = (int)skill;
        if ((uint)index >= (uint)_skillBlocks.Length)
        {
            return false;
        }

        var entry = _skillBlocks[index];
        if (entry != null)
        {
            entry.Active = active;
            UpdateSkillBlocksFlag();

            if (FeatureFlagSettings.LogChanges)
            {
                logger.Information("Skill block '{Skill}' set to {Active} by {ModifiedBy}", skill, active ? "ACTIVE" : "INACTIVE", modifiedBy);
            }

            if (FeatureFlagSettings.BroadcastChangesToStaff)
            {
                World.BroadcastStaff($"[Skill Block] '{skill}' set to {(active ? "ACTIVE" : "INACTIVE")} by {modifiedBy}");
            }

            SaveSkillBlocks();
            return true;
        }
        return false;
    }

    public static bool IsSpellBlocked(int spellId) =>
        _hasActiveSpellBlocks && (uint)spellId < (uint)_spellBlocks.Length && _spellBlocks[spellId] is { Active: true };

    public static bool IsSpellBlocked(Type spellType)
    {
        if (!_hasActiveSpellBlocks)
        {
            return false;
        }

        var id = SpellRegistry.GetRegistryNumber(spellType);
        return id >= 0 && (uint)id < (uint)_spellBlocks.Length && _spellBlocks[id] is { Active: true };
    }

    public static SpellBlockEntry GetSpellBlockEntry(int spellId) =>
        !_hasActiveSpellBlocks || (uint)spellId >= (uint)_spellBlocks.Length ? null : _spellBlocks[spellId];

    public static SpellBlockEntry GetSpellBlockEntry(Type spellType)
    {
        if (!_hasActiveSpellBlocks)
        {
            return null;
        }

        var id = SpellRegistry.GetRegistryNumber(spellType);
        return id >= 0 && (uint)id < (uint)_spellBlocks.Length ? _spellBlocks[id] : null;
    }

    public static IReadOnlyList<SpellBlockEntry> GetAllSpellBlocks()
    {
        var list = new List<SpellBlockEntry>();
        for (var i = 0; i < _spellBlocks.Length; i++)
        {
            if (_spellBlocks[i] != null)
            {
                list.Add(_spellBlocks[i]);
            }
        }
        return list;
    }

    public static void BlockSpell(Type spellType, string reason, string blockedBy = "System")
    {
        var spellId = SpellRegistry.GetRegistryNumber(spellType);
        if (spellId < 0 || (uint)spellId >= (uint)_spellBlocks.Length)
        {
            return;
        }

        var typeName = spellType.FullName ?? spellType.Name;
        var entry = new SpellBlockEntry
        {
            SpellType = spellType,
            SpellId = spellId,
            SpellTypeName = typeName,
            Reason = reason,
            Active = true,
            CreatedAt = Core.Now,
            CreatedBy = blockedBy
        };

        _spellBlocks[spellId] = entry;
        UpdateSpellBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Warning("Spell '{SpellType}' BLOCKED by {BlockedBy}. Reason: {Reason}", typeName, blockedBy, reason);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Spell Block] '{spellType.Name}' BLOCKED by {blockedBy}. Reason: {reason}");
        }

        SaveSpellBlocks();
    }

    public static bool BlockSpellByName(string typeName, string reason, string blockedBy = "System")
    {
        var type = ResolveType(typeName);
        if (type != null)
        {
            BlockSpell(type, reason, blockedBy);
            return true;
        }

        return false;
    }

    public static bool UnblockSpell(Type spellType, string unblockedBy = "System")
    {
        var spellId = SpellRegistry.GetRegistryNumber(spellType);
        if (spellId < 0 || (uint)spellId >= (uint)_spellBlocks.Length)
        {
            return false;
        }

        if (_spellBlocks[spellId] != null)
        {
            _spellBlocks[spellId] = null;
            UpdateSpellBlocksFlag();

            if (FeatureFlagSettings.LogChanges)
            {
                logger.Information("Spell '{SpellType}' UNBLOCKED by {UnblockedBy}", spellType.Name, unblockedBy);
            }

            if (FeatureFlagSettings.BroadcastChangesToStaff)
            {
                World.BroadcastStaff($"[Spell Block] '{spellType.Name}' UNBLOCKED by {unblockedBy}");
            }

            SaveSpellBlocks();
            return true;
        }
        return false;
    }

    public static bool UnblockSpellByName(string typeName, string unblockedBy = "System")
    {
        var type = ResolveType(typeName);
        return type != null && UnblockSpell(type, unblockedBy);
    }

    public static bool SetSpellBlockActive(int spellId, bool active, string modifiedBy = "System")
    {
        if ((uint)spellId >= (uint)_spellBlocks.Length)
        {
            return false;
        }

        var entry = _spellBlocks[spellId];
        if (entry != null)
        {
            entry.Active = active;
            UpdateSpellBlocksFlag();

            var typeName = entry.SpellType?.Name ?? entry.SpellTypeName;

            if (FeatureFlagSettings.LogChanges)
            {
                logger.Information("Spell block '{SpellType}' set to {Active} by {ModifiedBy}", typeName, active ? "ACTIVE" : "INACTIVE", modifiedBy);
            }

            if (FeatureFlagSettings.BroadcastChangesToStaff)
            {
                World.BroadcastStaff($"[Spell Block] '{typeName}' set to {(active ? "ACTIVE" : "INACTIVE")} by {modifiedBy}");
            }

            SaveSpellBlocks();
            return true;
        }

        return false;
    }

    private static bool CheckContainerAccess(Mobile mobile, Container container)
    {
        if (!_hasActiveContainerBlocks)
        {
            return true;
        }

        if (mobile.AccessLevel >= FeatureFlagSettings.RequiredAccessLevel)
        {
            return true;
        }

        if (_containerBlocks.TryGetValue(container.GetType(), out var entry) && entry.Active)
        {
            mobile.SendMessage(0x22, entry.Reason ?? FeatureFlagSettings.DefaultContainerBlockedMessage);
            return false;
        }

        return true;
    }

    public static bool IsContainerBlocked(Type containerType)
    {
        if (!_hasActiveContainerBlocks)
        {
            return false;
        }

        return _containerBlocks.TryGetValue(containerType, out var entry) && entry.Active;
    }

    public static ContainerBlockEntry GetContainerBlockEntry(Type containerType)
    {
        if (!_hasActiveContainerBlocks)
        {
            return null;
        }

        return _containerBlocks.GetValueOrDefault(containerType);
    }

    public static IReadOnlyCollection<ContainerBlockEntry> GetAllContainerBlocks() => _containerBlocks.Values;

    public static void BlockContainer(Type containerType, string reason, string blockedBy = "System")
    {
        var typeName = containerType.FullName ?? containerType.Name;
        var entry = new ContainerBlockEntry
        {
            ContainerType = containerType,
            ContainerTypeName = typeName,
            Reason = reason,
            Active = true,
            CreatedAt = Core.Now,
            CreatedBy = blockedBy
        };

        _containerBlocks[containerType] = entry;
        UpdateContainerBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Warning("Container '{ContainerType}' BLOCKED by {BlockedBy}. Reason: {Reason}", typeName, blockedBy, reason);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Container Block] '{containerType.Name}' BLOCKED by {blockedBy}. Reason: {reason}");
        }

        SaveContainerBlocks();
    }

    public static bool BlockContainerByName(string typeName, string reason, string blockedBy = "System")
    {
        var type = ResolveType(typeName);
        if (type == null || !typeof(Container).IsAssignableFrom(type))
        {
            return false;
        }

        BlockContainer(type, reason, blockedBy);
        return true;
    }

    public static bool UnblockContainer(Type containerType, string unblockedBy = "System")
    {
        if (_containerBlocks.Remove(containerType, out var removed))
        {
            UpdateContainerBlocksFlag();

            var typeName = removed.ContainerTypeName;

            if (FeatureFlagSettings.LogChanges)
            {
                logger.Information("Container '{ContainerType}' UNBLOCKED by {UnblockedBy}", typeName, unblockedBy);
            }

            if (FeatureFlagSettings.BroadcastChangesToStaff)
            {
                World.BroadcastStaff($"[Container Block] '{containerType.Name}' UNBLOCKED by {unblockedBy}");
            }

            SaveContainerBlocks();
            return true;
        }
        return false;
    }

    public static bool UnblockContainerByName(string typeName, string unblockedBy = "System")
    {
        var type = ResolveType(typeName);
        if (type != null && typeof(Container).IsAssignableFrom(type))
        {
            return UnblockContainer(type, unblockedBy);
        }

        return false;
    }

    public static bool SetContainerBlockActive(string typeName, bool active, string modifiedBy = "System")
    {
        foreach (var kvp in _containerBlocks)
        {
            if (string.Equals(kvp.Value.ContainerTypeName, typeName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(kvp.Key.Name, typeName, StringComparison.OrdinalIgnoreCase))
            {
                kvp.Value.Active = active;
                UpdateContainerBlocksFlag();

                if (FeatureFlagSettings.LogChanges)
                {
                    logger.Information("Container block '{ContainerType}' set to {Active} by {ModifiedBy}", typeName, active ? "ACTIVE" : "INACTIVE", modifiedBy);
                }

                if (FeatureFlagSettings.BroadcastChangesToStaff)
                {
                    World.BroadcastStaff($"[Container Block] '{typeName}' set to {(active ? "ACTIVE" : "INACTIVE")} by {modifiedBy}");
                }

                SaveContainerBlocks();
                return true;
            }
        }
        return false;
    }

    private static void LoadDefaultFlags()
    {
        var defaultFlagsPath = Path.Combine(Core.BaseDirectory, "Configuration", "FeatureFlags", "default-flags.json");
        var defaultFlags = JsonConfig.Deserialize<List<FeatureFlag>>(defaultFlagsPath);
        if (defaultFlags != null)
        {
            foreach (var flag in defaultFlags)
            {
                _flags.TryAdd(flag.Key, flag);
            }
        }
    }

    public static void Save()
    {
        SaveFlags();
        SaveGumpBlocks();
        SaveUseReqBlocks();
        SaveSkillBlocks();
        SaveSpellBlocks();
        SaveContainerBlocks();
    }

    private static void SaveFlags()
    {
        try
        {
            var flagsList = new List<FeatureFlag>(_flags.Values);
            JsonConfig.Serialize(Path.Combine(FeatureFlagSettings.SavePath, "flags.json"), flagsList);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save flags");
        }
    }

    private static void SaveGumpBlocks()
    {
        try
        {
            var list = new List<GumpBlockEntry>(_gumpBlocks.Values);
            JsonConfig.Serialize(Path.Combine(FeatureFlagSettings.SavePath, "gump-blocks.json"), list);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save gump blocks");
        }
    }

    private static void SaveUseReqBlocks()
    {
        try
        {
            var list = new List<UseReqBlockEntry>(_useReqBlocks.Values);
            JsonConfig.Serialize(Path.Combine(FeatureFlagSettings.SavePath, "usereq-blocks.json"), list);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save useReq blocks");
        }
    }

    private static void SaveSkillBlocks()
    {
        try
        {
            var list = new List<SkillBlockEntry>();
            for (var i = 0; i < _skillBlocks.Length; i++)
            {
                if (_skillBlocks[i] != null)
                {
                    list.Add(_skillBlocks[i]);
                }
            }
            JsonConfig.Serialize(Path.Combine(FeatureFlagSettings.SavePath, "skill-blocks.json"), list);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save skill blocks");
        }
    }

    private static void SaveSpellBlocks()
    {
        try
        {
            var list = new List<SpellBlockEntry>();
            for (var i = 0; i < _spellBlocks.Length; i++)
            {
                if (_spellBlocks[i] != null)
                {
                    list.Add(_spellBlocks[i]);
                }
            }
            JsonConfig.Serialize(Path.Combine(FeatureFlagSettings.SavePath, "spell-blocks.json"), list);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save spell blocks");
        }
    }

    private static void SaveContainerBlocks()
    {
        try
        {
            var list = new List<ContainerBlockEntry>(_containerBlocks.Values);
            JsonConfig.Serialize(Path.Combine(FeatureFlagSettings.SavePath, "container-blocks.json"), list);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save container blocks");
        }
    }

    public static void Load()
    {
        try
        {
            var savePath = FeatureFlagSettings.SavePath;

            // Load flags
            var flags = JsonConfig.Deserialize<List<FeatureFlag>>(Path.Combine(savePath, "flags.json"));
            if (flags != null)
            {
                foreach (var flag in flags)
                {
                    _flags[flag.Key] = flag;
                }
            }

            // Load gump blocks
            var gumpBlocks = JsonConfig.Deserialize<List<GumpBlockEntry>>(Path.Combine(savePath, "gump-blocks.json"));
            if (gumpBlocks != null)
            {
                foreach (var entry in gumpBlocks)
                {
                    var type = ResolveType(entry.GumpTypeName);
                    if (type != null)
                    {
                        entry.GumpType = type;
                        _gumpBlocks[type] = entry;
                    }
                    else
                    {
                        logger.Warning("Could not resolve gump type '{GumpType}', skipping", entry.GumpTypeName);
                    }
                }
            }

            // Load useReq blocks
            var useReqBlocks = JsonConfig.Deserialize<List<UseReqBlockEntry>>(Path.Combine(savePath, "usereq-blocks.json"));
            if (useReqBlocks != null)
            {
                foreach (var entry in useReqBlocks)
                {
                    var type = ResolveType(entry.ItemTypeName);
                    if (type != null)
                    {
                        entry.ItemType = type;
                        _useReqBlocks[type] = entry;
                    }
                    else
                    {
                        logger.Warning("Could not resolve item type '{ItemType}', skipping", entry.ItemTypeName);
                    }
                }
            }

            // Load skill blocks
            var skillBlocks = JsonConfig.Deserialize<List<SkillBlockEntry>>(Path.Combine(savePath, "skill-blocks.json"));
            if (skillBlocks != null)
            {
                foreach (var entry in skillBlocks)
                {
                    if (Enum.TryParse<SkillName>(entry.SkillName, true, out var skill))
                    {
                        var index = (int)skill;
                        if ((uint)index < (uint)_skillBlocks.Length)
                        {
                            entry.Skill = skill;
                            _skillBlocks[index] = entry;
                        }
                    }
                    else
                    {
                        logger.Warning("Could not resolve skill '{SkillName}', skipping", entry.SkillName);
                    }
                }
            }

            // Load spell blocks
            var spellBlocks = JsonConfig.Deserialize<List<SpellBlockEntry>>(Path.Combine(savePath, "spell-blocks.json"));
            if (spellBlocks != null)
            {
                foreach (var entry in spellBlocks)
                {
                    var type = ResolveType(entry.SpellTypeName);
                    if (type == null)
                    {
                        logger.Warning("Could not resolve spell type '{SpellType}', skipping", entry.SpellTypeName);
                        continue;
                    }

                    var spellId = SpellRegistry.GetRegistryNumber(type);
                    if (spellId < 0 || (uint)spellId >= (uint)_spellBlocks.Length)
                    {
                        logger.Warning("Spell type '{SpellType}' has no registered spell ID, skipping", entry.SpellTypeName);
                        continue;
                    }

                    entry.SpellType = type;
                    entry.SpellId = spellId;
                    _spellBlocks[spellId] = entry;
                }
            }

            // Load container blocks
            var containerBlocks = JsonConfig.Deserialize<List<ContainerBlockEntry>>(Path.Combine(savePath, "container-blocks.json"));
            if (containerBlocks != null)
            {
                foreach (var entry in containerBlocks)
                {
                    var type = ResolveType(entry.ContainerTypeName);
                    if (type != null)
                    {
                        entry.ContainerType = type;
                        _containerBlocks[type] = entry;
                    }
                    else
                    {
                        logger.Warning("Could not resolve container type '{ContainerType}', skipping", entry.ContainerTypeName);
                    }
                }
            }

            // Update fast-bailout flags
            UpdateGumpBlocksFlag();
            UpdateUseReqBlocksFlag();
            UpdateSkillBlocksFlag();
            UpdateSpellBlocksFlag();
            UpdateContainerBlocksFlag();

            // Sync static bool flags
            SyncAllStaticFlags();

            logger.Debug("Feature flags loaded successfully");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to load feature flags");
        }
    }

    private static Type ResolveType(string typeName) =>
        AssemblyHandler.FindTypeByFullName(typeName) ?? AssemblyHandler.FindTypeByName(typeName);

    private static void SyncStaticFlag(string key, bool enabled)
    {
        switch (key.ToLowerInvariant())
        {
            // Server project flags
            case "player_trading":  ServerFeatureFlags.PlayerTrading = enabled; break;
            case "pvp_combat":      ServerFeatureFlags.PvPCombat = enabled; break;

            // UOContent flags
            case "vendor_purchase": ContentFeatureFlags.VendorPurchase = enabled; break;
            case "vendor_sell":     ContentFeatureFlags.VendorSell = enabled; break;
            case "player_vendors":  ContentFeatureFlags.PlayerVendors = enabled; break;
            case "house_placement": ContentFeatureFlags.HousePlacement = enabled; break;
            case "bulk_orders":     ContentFeatureFlags.BulkOrders = enabled; break;
        }
    }

    private static void SyncAllStaticFlags()
    {
        foreach (var flag in _flags.Values)
        {
            SyncStaticFlag(flag.Key, flag.Enabled);
        }
    }

    private static void UpdateGumpBlocksFlag()
    {
        foreach (var entry in _gumpBlocks.Values)
        {
            if (entry.Active)
            {
                _hasActiveGumpBlocks = true;
                return;
            }
        }
        _hasActiveGumpBlocks = false;
    }

    private static void UpdateUseReqBlocksFlag()
    {
        foreach (var entry in _useReqBlocks.Values)
        {
            if (entry.Active)
            {
                _hasActiveUseReqBlocks = true;
                return;
            }
        }
        _hasActiveUseReqBlocks = false;
    }

    private static void UpdateSkillBlocksFlag()
    {
        for (var i = 0; i < _skillBlocks.Length; i++)
        {
            if (_skillBlocks[i] is { Active: true })
            {
                _hasActiveSkillBlocks = true;
                return;
            }
        }
        _hasActiveSkillBlocks = false;
    }

    private static void UpdateSpellBlocksFlag()
    {
        for (var i = 0; i < _spellBlocks.Length; i++)
        {
            if (_spellBlocks[i] is { Active: true })
            {
                _hasActiveSpellBlocks = true;
                return;
            }
        }
        _hasActiveSpellBlocks = false;
    }

    private static void UpdateContainerBlocksFlag()
    {
        foreach (var entry in _containerBlocks.Values)
        {
            if (entry.Active)
            {
                _hasActiveContainerBlocks = true;
                return;
            }
        }
        _hasActiveContainerBlocks = false;
    }
}
