using System;
using System.Collections.Generic;
using System.IO;
using Server.Gumps;
using Server.Json;
using Server.Logging;
using Server.Spells;

namespace Server.Systems.FeatureFlags;

public static class FeatureFlagManager
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FeatureFlagManager));

    // Primary storage
    private static readonly Dictionary<string, FeatureFlag> _flags = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<Type, GumpBlockEntry> _gumpBlocks = new();
    private static readonly Dictionary<Type, ItemBlockEntry> _itemBlocks = new();
    private static readonly SpellBlockEntry[] _spellBlocks = new SpellBlockEntry[SpellRegistry.Types.Length];
    private static readonly SkillBlockEntry[] _skillBlocks = new SkillBlockEntry[58];

    // Fast bailout flags
    private static bool _hasActiveGumpBlocks;
    private static bool _hasActiveItemBlocks;
    private static bool _hasActiveSkillBlocks;
    private static bool _hasActiveSpellBlocks;

    private static bool _initialized;

    public static void Initialize()
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

        _initialized = true;
        logger.Information(
            "Feature Flag system initialized with {FlagCount} flags, {GumpBlockCount} gump blocks, {ItemBlockCount} item blocks, {SkillBlockCount} skill blocks, {SpellBlockCount} spell blocks",
            _flags.Count, _gumpBlocks.Count, _itemBlocks.Count, CountActiveSkillBlocks(), CountActiveSpellBlocks());
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

    public static bool IsGumpBlocked<T>() where T : BaseGump =>
        _hasActiveGumpBlocks && _gumpBlocks.TryGetValue(typeof(T), out var entry) && entry.Active;

    public static bool IsGumpBlocked(Type gumpType) =>
        _hasActiveGumpBlocks && _gumpBlocks.TryGetValue(gumpType, out var entry) && entry.Active;

    public static GumpBlockEntry GetGumpBlockEntry(Type gumpType) =>
        _hasActiveGumpBlocks ? _gumpBlocks.GetValueOrDefault(gumpType) : null;

    public static IReadOnlyCollection<GumpBlockEntry> GetAllGumpBlocks() => _gumpBlocks.Values;

    public static void BlockGump<T>(string reason, string blockedBy = "System") where T : BaseGump =>
        BlockGump(typeof(T), reason, blockedBy);

    public static void BlockGump(Type gumpType, string reason, string blockedBy = "System")
    {
        var entry = new GumpBlockEntry
        {
            ResolvedType = gumpType,
            Reason = reason,
            Active = true,
            CreatedAt = Core.Now,
            CreatedBy = blockedBy
        };

        _gumpBlocks[gumpType] = entry;
        UpdateGumpBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Warning("Gump '{GumpType}' BLOCKED by {BlockedBy}. Reason: {Reason}", gumpType.FullName, blockedBy, reason);
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
        if (!_gumpBlocks.Remove(gumpType))
        {
            return false;
        }

        UpdateGumpBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Information("Gump '{GumpType}' UNBLOCKED by {UnblockedBy}", gumpType.FullName, unblockedBy);
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
        return type != null && SetGumpBlockActive(type, active, modifiedBy);
    }

    public static bool SetGumpBlockActive(Type gumpType, bool active, string modifiedBy = "System")
    {
        if (!_gumpBlocks.TryGetValue(gumpType, out var entry))
        {
            return false;
        }

        entry.Active = active;
        UpdateGumpBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Information("Gump block '{GumpType}' set to {Active} by {ModifiedBy}", gumpType.FullName, active ? "ACTIVE" : "INACTIVE", modifiedBy);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Gump Block] '{gumpType.Name}' set to {(active ? "ACTIVE" : "INACTIVE")} by {modifiedBy}");
        }

        SaveGumpBlocks();
        return true;
    }

    public static bool IsItemUseBlocked(Type itemType, out string reason)
    {
        if (_hasActiveItemBlocks && _itemBlocks.TryGetValue(itemType, out var entry) && entry.Active &&
            entry.BlockUse)
        {
            reason = entry.Reason ?? FeatureFlagSettings.DefaultItemUseBlockedMessage;
            return true;
        }

        reason = null;
        return false;
    }

    public static bool IsItemEquipBlocked(Type itemType, out string reason)
    {
        if (_hasActiveItemBlocks && _itemBlocks.TryGetValue(itemType, out var entry) && entry.Active &&
            entry.BlockEquip)
        {
            reason = entry.Reason ?? FeatureFlagSettings.DefaultItemEquipBlockedMessage;
            return true;
        }

        reason = null;
        return false;
    }

    public static bool IsContainerAccessBlocked(Type containerType, out string reason)
    {
        if (_hasActiveItemBlocks && _itemBlocks.TryGetValue(containerType, out var entry) && entry.Active &&
            entry.BlockContainerAccess)
        {
            reason = entry.Reason ?? FeatureFlagSettings.DefaultContainerBlockedMessage;
            return true;
        }

        reason = null;
        return false;
    }

    public static ItemBlockEntry GetItemBlockEntry(Type itemType) =>
        _hasActiveItemBlocks ? _itemBlocks.GetValueOrDefault(itemType) : null;

    public static IReadOnlyCollection<ItemBlockEntry> GetAllItemBlocks() => _itemBlocks.Values;

    public static void BlockItemUse(Type itemType, string reason, string blockedBy = "System") =>
        SetItemBlockFlag(itemType, "Use", reason, blockedBy, (e, v) => e.BlockUse = v);

    public static void BlockItemEquip(Type itemType, string reason, string blockedBy = "System") =>
        SetItemBlockFlag(itemType, "Equip", reason, blockedBy, (e, v) => e.BlockEquip = v);

    public static void BlockItemContainer(Type itemType, string reason, string blockedBy = "System") =>
        SetItemBlockFlag(itemType, "Container", reason, blockedBy, (e, v) => e.BlockContainerAccess = v);

    public static bool BlockItemUseByName(string typeName, string reason, string blockedBy = "System") =>
        ResolveItemType(typeName, out var type) && Apply(() => BlockItemUse(type, reason, blockedBy));

    public static bool BlockItemEquipByName(string typeName, string reason, string blockedBy = "System") =>
        ResolveItemType(typeName, out var type) && Apply(() => BlockItemEquip(type, reason, blockedBy));

    public static bool BlockItemContainerByName(string typeName, string reason, string blockedBy = "System") =>
        ResolveItemType(typeName, out var type) && Apply(() => BlockItemContainer(type, reason, blockedBy));

    public static bool UnblockItemUse(Type itemType, string unblockedBy = "System") =>
        ClearItemBlockFlag(itemType, "Use", unblockedBy, (e, v) => e.BlockUse = v, e => e.BlockUse);

    public static bool UnblockItemEquip(Type itemType, string unblockedBy = "System") =>
        ClearItemBlockFlag(itemType, "Equip", unblockedBy, (e, v) => e.BlockEquip = v, e => e.BlockEquip);

    public static bool UnblockItemContainer(Type itemType, string unblockedBy = "System") =>
        ClearItemBlockFlag(itemType, "Container", unblockedBy, (e, v) => e.BlockContainerAccess = v, e => e.BlockContainerAccess);

    public static bool UnblockItemUseByName(string typeName, string unblockedBy = "System") =>
        ResolveItemType(typeName, out var type) && UnblockItemUse(type, unblockedBy);

    public static bool UnblockItemEquipByName(string typeName, string unblockedBy = "System") =>
        ResolveItemType(typeName, out var type) && UnblockItemEquip(type, unblockedBy);

    public static bool UnblockItemContainerByName(string typeName, string unblockedBy = "System") =>
        ResolveItemType(typeName, out var type) && UnblockItemContainer(type, unblockedBy);

    private static bool ResolveItemType(string typeName, out Type type)
    {
        type = ResolveType(typeName);
        return type != null && typeof(Item).IsAssignableFrom(type);
    }

    private static bool Apply(Action action)
    {
        action();
        return true;
    }

    private static void SetItemBlockFlag(
        Type itemType, string action, string reason, string blockedBy,
        Action<ItemBlockEntry, bool> setter)
    {
        if (!_itemBlocks.TryGetValue(itemType, out var entry))
        {
            entry = new ItemBlockEntry
            {
                ResolvedType = itemType,
                Active = true,
                CreatedAt = Core.Now,
                CreatedBy = blockedBy
            };
            _itemBlocks[itemType] = entry;
        }

        setter(entry, true);

        if (reason != null)
        {
            entry.Reason = reason;
        }

        UpdateItemBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Warning("Item '{ItemType}' {Action} BLOCKED by {BlockedBy}. Reason: {Reason}", itemType.FullName, action, blockedBy, reason);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Item Block] '{itemType.Name}' {action} BLOCKED by {blockedBy}. Reason: {reason}");
        }

        SaveItemBlocks();
    }

    private static bool ClearItemBlockFlag(
        Type itemType, string action, string unblockedBy,
        Action<ItemBlockEntry, bool> setter, Func<ItemBlockEntry, bool> getter)
    {
        if (!_itemBlocks.TryGetValue(itemType, out var entry) || !getter(entry))
        {
            return false;
        }

        setter(entry, false);

        // Remove entry entirely if no flags remain
        if (!entry.BlockUse && !entry.BlockEquip && !entry.BlockContainerAccess)
        {
            _itemBlocks.Remove(itemType);
        }

        UpdateItemBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Information("Item '{ItemType}' {Action} UNBLOCKED by {UnblockedBy}", itemType.FullName, action, unblockedBy);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Item Block] '{itemType.Name}' {action} UNBLOCKED by {unblockedBy}");
        }

        SaveItemBlocks();
        return true;
    }

    public static bool RemoveItemBlock(Type itemType, string removedBy = "System")
    {
        if (!_itemBlocks.Remove(itemType))
        {
            return false;
        }

        UpdateItemBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Information("Item '{ItemType}' block REMOVED by {RemovedBy}", itemType.FullName, removedBy);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Item Block] '{itemType.Name}' REMOVED by {removedBy}");
        }

        SaveItemBlocks();
        return true;
    }

    public static bool SetItemBlockActive(Type itemType, bool active, string modifiedBy = "System")
    {
        if (!_itemBlocks.TryGetValue(itemType, out var entry))
        {
            return false;
        }

        entry.Active = active;
        UpdateItemBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Information("Item block '{ItemType}' set to {Active} by {ModifiedBy}", itemType.FullName, active ? "ACTIVE" : "INACTIVE", modifiedBy);
        }

        if (FeatureFlagSettings.BroadcastChangesToStaff)
        {
            World.BroadcastStaff($"[Item Block] '{itemType.Name}' set to {(active ? "ACTIVE" : "INACTIVE")} by {modifiedBy}");
        }

        SaveItemBlocks();
        return true;
    }

    public static bool IsSkillBlocked(SkillName skill, out string reason)
    {
        var index = (int)skill;
        if (!_hasActiveSkillBlocks || index < 0 || index >= _skillBlocks.Length
            || _skillBlocks[index] is not { Active: true } entry)
        {
            reason = null;
            return false;
        }

        reason = entry.Reason ?? FeatureFlagSettings.DefaultSkillDisabledMessage;
        return entry is { Active: true };
    }

    public static SkillBlockEntry GetSkillBlockEntry(SkillName skill)
    {
        var index = (int)skill;
        return index >= 0 && index < _skillBlocks.Length ? _skillBlocks[index] : null;
    }

    // NOTE: Will contain nulls!
    public static ReadOnlySpan<SkillBlockEntry> GetAllSkillBlocks() => _skillBlocks;

    public static void BlockSkill(SkillName skill, string reason, string blockedBy = "System")
    {
        var index = (int)skill;
        if (index < 0 || index >= _skillBlocks.Length)
        {
            return;
        }

        var entry = new SkillBlockEntry
        {
            Skill = skill,
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
        if (index < 0 || index >= _skillBlocks.Length)
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
        if (index < 0 || index >= _skillBlocks.Length)
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

    public static bool IsSpellBlocked(int spellId, out string reason)
    {
        if (!_hasActiveSpellBlocks || spellId < 0 || spellId >= _spellBlocks.Length ||
            _spellBlocks[spellId] is not { Active: true } entry)
        {
            reason = null;
            return false;
        }

        reason = entry.Reason ?? FeatureFlagSettings.DefaultSpellDisabledMessage;
        return true;
    }

    public static bool IsSpellBlocked(Type spellType)
    {
        if (!_hasActiveSpellBlocks)
        {
            return false;
        }

        var id = SpellRegistry.GetRegistryNumber(spellType);
        return id >= 0 && id < _spellBlocks.Length && _spellBlocks[id] is { Active: true };
    }

    public static SpellBlockEntry GetSpellBlockEntry(int spellId) =>
        !_hasActiveSpellBlocks || spellId >= 0 && spellId >= _spellBlocks.Length ? null : _spellBlocks[spellId];

    public static SpellBlockEntry GetSpellBlockEntry(Type spellType)
    {
        if (!_hasActiveSpellBlocks)
        {
            return null;
        }

        var id = SpellRegistry.GetRegistryNumber(spellType);
        return id >= 0 && id < _spellBlocks.Length ? _spellBlocks[id] : null;
    }

    // NOTE: Will contain nulls!
    public static ReadOnlySpan<SpellBlockEntry> GetAllSpellBlocks() => _spellBlocks;

    public static void BlockSpell(Type spellType, string reason, string blockedBy = "System")
    {
        var spellId = SpellRegistry.GetRegistryNumber(spellType);
        if (spellId < 0 || spellId >= _spellBlocks.Length)
        {
            return;
        }

        var entry = new SpellBlockEntry
        {
            ResolvedType = spellType,
            SpellId = spellId,
            Reason = reason,
            Active = true,
            CreatedAt = Core.Now,
            CreatedBy = blockedBy
        };

        _spellBlocks[spellId] = entry;
        UpdateSpellBlocksFlag();

        if (FeatureFlagSettings.LogChanges)
        {
            logger.Warning("Spell '{SpellType}' BLOCKED by {BlockedBy}. Reason: {Reason}", spellType.FullName, blockedBy, reason);
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
        if (spellId < 0 || spellId >= _spellBlocks.Length)
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
        if (spellId < 0 || spellId >= _spellBlocks.Length)
        {
            return false;
        }

        var entry = _spellBlocks[spellId];
        if (entry != null)
        {
            entry.Active = active;
            UpdateSpellBlocksFlag();

            var typeName = entry.DisplayName;

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
        SaveItemBlocks();
        SaveSkillBlocks();
        SaveSpellBlocks();
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

    private static void SaveItemBlocks()
    {
        try
        {
            var list = new List<ItemBlockEntry>(_itemBlocks.Values);
            JsonConfig.Serialize(Path.Combine(FeatureFlagSettings.SavePath, "item-blocks.json"), list);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to save item blocks");
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

            // Load gump blocks (TypeConverter resolves Type from JSON)
            var gumpBlocks = JsonConfig.Deserialize<List<GumpBlockEntry>>(Path.Combine(savePath, "gump-blocks.json"));
            if (gumpBlocks != null)
            {
                for (var i = 0; i < gumpBlocks.Count; i++)
                {
                    var entry = gumpBlocks[i];
                    if (entry.ResolvedType != null)
                    {
                        _gumpBlocks[entry.ResolvedType] = entry;
                    }
                }
            }

            // Load item blocks (or migrate from old format)
            var itemBlocksPath = Path.Combine(savePath, "item-blocks.json");
            var itemBlocks = JsonConfig.Deserialize<List<ItemBlockEntry>>(itemBlocksPath);
            if (itemBlocks != null)
            {
                for (var i = 0; i < itemBlocks.Count; i++)
                {
                    var entry = itemBlocks[i];
                    if (entry.ResolvedType != null)
                    {
                        _itemBlocks[entry.ResolvedType] = entry;
                    }
                }
            }

            // Load skill blocks (JsonStringEnumConverter deserializes SkillName)
            var skillBlocks = JsonConfig.Deserialize<List<SkillBlockEntry>>(Path.Combine(savePath, "skill-blocks.json"));
            if (skillBlocks != null)
            {
                for (var i = 0; i < skillBlocks.Count; i++)
                {
                    var entry = skillBlocks[i];
                    var index = (int)entry.Skill;
                    if (index >= 0 && index < _skillBlocks.Length)
                    {
                        _skillBlocks[index] = entry;
                    }
                }
            }

            // Load spell blocks (TypeConverter resolves Type, then look up SpellId)
            var spellBlocks = JsonConfig.Deserialize<List<SpellBlockEntry>>(Path.Combine(savePath, "spell-blocks.json"));
            if (spellBlocks != null)
            {
                for (var i = 0; i < spellBlocks.Count; i++)
                {
                    var entry = spellBlocks[i];
                    if (entry.ResolvedType == null)
                    {
                        continue;
                    }

                    var spellId = SpellRegistry.GetRegistryNumber(entry.ResolvedType);
                    if (spellId < 0 || spellId >= _spellBlocks.Length)
                    {
                        logger.Warning(
                            "Spell type '{SpellType}' has no registered spell ID, skipping",
                            entry.ResolvedType.FullName
                        );
                        continue;
                    }

                    entry.SpellId = spellId;
                    _spellBlocks[spellId] = entry;
                }
            }

            // Update fast-bailout flags
            UpdateGumpBlocksFlag();
            UpdateItemBlocksFlag();
            UpdateSkillBlocksFlag();
            UpdateSpellBlocksFlag();

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
        _ = key.ToLowerInvariant() switch
        {
            // Server project flags
            "player_trading" => ServerFeatureFlags.PlayerTrading = enabled,
            "pvp_combat"     => ServerFeatureFlags.PvPCombat = enabled,
            "bank_access"    => ServerFeatureFlags.BankAccess = enabled,

            // UOContent flags
            "vendor_purchase" => ContentFeatureFlags.VendorPurchase = enabled,
            "vendor_sell"     => ContentFeatureFlags.VendorSell = enabled,
            "player_vendors"  => ContentFeatureFlags.PlayerVendors = enabled,
            "house_placement" => ContentFeatureFlags.HousePlacement = enabled,
            "boat_placement"  => ContentFeatureFlags.BoatPlacement = enabled,
            "bulk_orders"     => ContentFeatureFlags.BulkOrders = enabled,
        };
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

    private static void UpdateItemBlocksFlag()
    {
        foreach (var entry in _itemBlocks.Values)
        {
            if (entry.Active)
            {
                _hasActiveItemBlocks = true;
                return;
            }
        }
        _hasActiveItemBlocks = false;
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
}
