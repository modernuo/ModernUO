using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Mobiles;

namespace Server.Commands;

public static class StaffAccess
{
    private static readonly Dictionary<string, AccessLevel> _accessLevelByString = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string AccountTag(Serial serial) => $"OriginalStaffAccess:{serial}";

    public static void Initialize()
    {
        CommandSystem.Register("StaffAccess", AccessLevel.Player, StaffAccess_OnCommand);

        foreach (var accessLevel in Enum.GetValues<AccessLevel>())
        {
            _accessLevelByString[accessLevel.ToString().ToLower()] = accessLevel;
        }

        _accessLevelByString["gm"] = AccessLevel.GameMaster;
        _accessLevelByString["dev"] = AccessLevel.Developer;
        _accessLevelByString["admin"] = AccessLevel.Administrator;
    }

    public static void ResetStaffAccess(this PlayerMobile m)
    {
        if (m.Account is not Account account)
        {
            return;
        }

        var accountTag = AccountTag(m.Serial);
        var originalAccessLevelString = account.GetTag(accountTag);
        if (originalAccessLevelString == null)
        {
            return;
        }

        var accessLevel = _accessLevelByString[originalAccessLevelString];
        account.RemoveTag(accountTag);
        m.AccessLevel = accessLevel;
    }

    [Usage("StaffAccess <access level>")]
    [Description("Overrides your access level.")]
    public static void StaffAccess_OnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;
        if (m.Account is not Account account)
        {
            return;
        }

        var accountTag = AccountTag(m.Serial);
        var originalAccessLevelString = account.GetTag(accountTag);
        AccessLevel? originalAccessLevel = originalAccessLevelString != null ? _accessLevelByString[originalAccessLevelString] : null;
        if (originalAccessLevel == null && m.AccessLevel == AccessLevel.Player)
        {
            return;
        }

        var accessLevelArgument = e.GetString(0)?.Trim().ToLower();
        AccessLevel newAccessLevel = AccessLevel.Player;
        var validAccessLevel = !string.IsNullOrEmpty(accessLevelArgument) &&
                               _accessLevelByString.TryGetValue(accessLevelArgument, out newAccessLevel);

        if (!validAccessLevel && originalAccessLevel == null)
        {
            m.SendMessage("Invalid access level specified.");
            m.SendMessage("Usage: [staffaccess <access level>.");
            return;
        }

        if (originalAccessLevel != null && (!validAccessLevel || newAccessLevel == originalAccessLevel))
        {
            account.RemoveTag(accountTag);
            newAccessLevel = originalAccessLevel.Value;
            m.SendMessage("Restoring original staff access...");
        }

        if ((originalAccessLevel ?? m.AccessLevel) < newAccessLevel)
        {
            m.SendMessage($"You cannot set your staff access to {newAccessLevel.ToString()}.");
            return;
        }

        if (originalAccessLevel == null)
        {
            // Save the original access level
            account.AddTag(accountTag, m.AccessLevel.ToString().ToLower());
        }

        m.AccessLevel = newAccessLevel;
        m.SendMessage($"Staff access set to {newAccessLevel.ToString()}.");
    }
}
