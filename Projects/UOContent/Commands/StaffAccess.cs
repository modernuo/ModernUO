using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Accounting;
using Server.Mobiles;

namespace Server.Commands;

public static class StaffAccess
{
    // Legacy saves hold the original level in this account tag. Staff can edit tags, so Initialize discards them.
    private const string LegacyTagPrefix = "OriginalStaffAccess:";

    private static readonly Dictionary<string, AccessLevel> _accessLevelByString = BuildAccessLevelLookup();

    // Null until the first [StaffAccess so shards that never use it carry no state and write no save file.
    private static Dictionary<PlayerMobile, AccessLevel> _originalLevels;
    private static StaffAccessPersistence _persistence;

    public static void Configure()
    {
        CommandSystem.Register("StaffAccess", AccessLevel.Player, StaffAccess_OnCommand);
        _persistence = new StaffAccessPersistence();
    }

    private static Dictionary<string, AccessLevel> BuildAccessLevelLookup()
    {
        var lookup = new Dictionary<string, AccessLevel>();

        foreach (var accessLevel in Enum.GetValues<AccessLevel>())
        {
            lookup[accessLevel.ToString().ToLower()] = accessLevel;
        }

        lookup["gm"] = AccessLevel.GameMaster;
        lookup["dev"] = AccessLevel.Developer;
        lookup["admin"] = AccessLevel.Administrator;

        return lookup;
    }

    public static void Initialize()
    {
        // Legacy tag values are not trusted; affected staff are restored by an Owner.
        foreach (var account in Accounts.GetAccounts())
        {
            if (account is not Account acct)
            {
                continue;
            }

            if (acct.Tags.RemoveAll(static t => t.Name?.StartsWith(LegacyTagPrefix, StringComparison.Ordinal) == true) > 0)
            {
                acct.MarkDirty();
            }
        }
    }

    public static bool TryGetOriginalAccessLevel(PlayerMobile m, out AccessLevel level)
    {
        if (_originalLevels != null && _originalLevels.TryGetValue(m, out level))
        {
            return true;
        }

        level = AccessLevel.Player;
        return false;
    }

    private static void SetOriginalAccessLevel(PlayerMobile m, AccessLevel level) =>
        (_originalLevels ??= new Dictionary<PlayerMobile, AccessLevel>())[m] = level;

    private static bool RemoveOriginalAccessLevel(PlayerMobile m, out AccessLevel level)
    {
        if (_originalLevels?.Remove(m, out level) != true)
        {
            level = AccessLevel.Player;
            return false;
        }

        if (_originalLevels.Count == 0)
        {
            _originalLevels = null;
        }

        return true;
    }

    /// <summary>
    /// Called whenever <paramref name="m"/>'s access level changes. A change made by anything other than
    /// [StaffAccess cancels the pending restore, so a demotion by higher staff cannot be undone by the player.
    /// </summary>
    public static void OnAccessLevelChanged(PlayerMobile m) => RemoveOriginalAccessLevel(m, out _);

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(PlayerMobile m) => RemoveOriginalAccessLevel(m, out _);

    public static void ResetStaffAccess(this PlayerMobile m)
    {
        if (RemoveOriginalAccessLevel(m, out var accessLevel))
        {
            m.AccessLevel = accessLevel;
        }
    }

    [Usage("StaffAccess <access level>")]
    [Description("Changes your access level. Only access levels at or below your current access level can be set. Use the same access level to reset.")]
    public static void StaffAccess_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile m)
        {
            return;
        }

        var hasOriginal = TryGetOriginalAccessLevel(m, out var originalAccessLevel);
        if (!hasOriginal && m.AccessLevel == AccessLevel.Player)
        {
            return;
        }

        var accessLevelArgument = e.GetString(0)?.Trim().ToLower();
        var newAccessLevel = AccessLevel.Player;
        var validAccessLevel = !string.IsNullOrEmpty(accessLevelArgument) &&
                               _accessLevelByString.TryGetValue(accessLevelArgument, out newAccessLevel);

        if (!validAccessLevel && !hasOriginal)
        {
            m.SendMessage("Invalid access level specified.");
            m.SendMessage("Usage: [staffaccess <access level>.");
            return;
        }

        if (hasOriginal && (!validAccessLevel || newAccessLevel == originalAccessLevel))
        {
            m.SendMessage("Restoring original staff access...");
            m.ResetStaffAccess();
            m.SendMessage($"Staff access set to {originalAccessLevel}.");
            return;
        }

        var ceiling = hasOriginal ? originalAccessLevel : m.AccessLevel;
        if (ceiling < newAccessLevel)
        {
            m.SendMessage($"You cannot set your staff access to {newAccessLevel}.");
            return;
        }

        // Assigning first: the change notification clears any entry, then the original is recorded again.
        m.AccessLevel = newAccessLevel;

        if (newAccessLevel != ceiling)
        {
            SetOriginalAccessLevel(m, ceiling);
        }

        m.SendMessage($"Staff access set to {newAccessLevel}.");
    }

    internal static void Serialize(IGenericWriter writer)
    {
        if (_originalLevels is not { Count: > 0 })
        {
            return;
        }

        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(_originalLevels.Count);

        foreach (var (m, level) in _originalLevels)
        {
            writer.Write(m);
            writer.Write((byte)level);
        }
    }

    internal static void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version
        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var m = reader.ReadEntity<PlayerMobile>();
            var level = (AccessLevel)reader.ReadByte();

            if (m?.Deleted == false)
            {
                SetOriginalAccessLevel(m, level);
            }
        }
    }
}

public class StaffAccessPersistence : GenericPersistence
{
    public StaffAccessPersistence() : base("StaffAccess", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => StaffAccess.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => StaffAccess.Deserialize(reader);
}
