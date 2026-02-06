using System;
using Server.Gumps;

namespace Server.Systems.FeatureFlags;

public static class FeatureFlagExtensions
{
    public static bool TrySendGump(this Mobile mobile, BaseGump gump, bool singleton = false)
    {
        if (mobile == null || gump == null)
        {
            return false;
        }

        // Staff bypass
        if (mobile.AccessLevel >= FeatureFlagSettings.RequiredAccessLevel)
        {
            mobile.SendGump(gump, singleton);
            return true;
        }

        // Single lookup - check block entry directly
        var blockEntry = FeatureFlagManager.GetGumpBlockEntry(gump.GetType());
        if (blockEntry is { Active: true })
        {
            mobile.SendMessage(0x22, blockEntry.Reason ?? FeatureFlagSettings.DefaultGumpBlockedMessage);
            return false;
        }

        mobile.SendGump(gump, singleton);
        return true;
    }

    public static bool CanSendGump<T>(this Mobile mobile, bool sendMessageIfBlocked = false) where T : BaseGump
    {
        if (mobile == null)
        {
            return false;
        }

        if (mobile.AccessLevel >= FeatureFlagSettings.RequiredAccessLevel)
        {
            return true;
        }

        if (FeatureFlagManager.IsGumpBlocked<T>())
        {
            if (sendMessageIfBlocked)
            {
                var blockEntry = FeatureFlagManager.GetGumpBlockEntry(typeof(T));
                mobile.SendMessage(0x22, blockEntry?.Reason ?? FeatureFlagSettings.DefaultGumpBlockedMessage);
            }
            return false;
        }

        return true;
    }

    public static bool CanBeUsed(this Item item, Mobile from, bool sendMessageIfBlocked = true)
    {
        if (item == null || from == null)
        {
            return false;
        }

        if (from.AccessLevel >= FeatureFlagSettings.RequiredAccessLevel)
        {
            return true;
        }

        var blockReason = FeatureFlagManager.CheckUseReq(item, from, sendMessageIfBlocked);
        return blockReason == null;
    }

    public static bool IsFeatureEnabled(this Mobile mobile, string flagKey)
    {
        if (mobile == null)
        {
            return FeatureFlagManager.IsEnabled(flagKey);
        }

        if (mobile.AccessLevel >= FeatureFlagSettings.RequiredAccessLevel)
        {
            return true;
        }

        return FeatureFlagManager.IsEnabled(flagKey);
    }

    public static bool IsSkillEnabled(this Mobile mobile, SkillName skill)
    {
        if (mobile == null)
        {
            return !FeatureFlagManager.IsSkillBlocked(skill);
        }

        if (mobile.AccessLevel >= FeatureFlagSettings.RequiredAccessLevel)
        {
            return true;
        }

        return !FeatureFlagManager.IsSkillBlocked(skill);
    }

    public static bool IsSpellEnabled(this Mobile mobile, Type spellType)
    {
        if (mobile == null)
        {
            return !FeatureFlagManager.IsSpellBlocked(spellType);
        }

        if (mobile.AccessLevel >= FeatureFlagSettings.RequiredAccessLevel)
        {
            return true;
        }

        return !FeatureFlagManager.IsSpellBlocked(spellType);
    }
}
