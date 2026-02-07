using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Targeting;

namespace Server.Systems.FeatureFlags;

public static class FeatureFlagCommands
{
    public static void Configure()
    {
        CommandSystem.Register("FeatureFlag", AccessLevel.Administrator, FeatureFlag_OnCommand);
        CommandSystem.Register("FF", AccessLevel.Administrator, FeatureFlag_OnCommand);
        CommandSystem.Register("BlockGump", AccessLevel.Administrator, BlockGump_OnCommand);
        CommandSystem.Register("UnblockGump", AccessLevel.Administrator, UnblockGump_OnCommand);
        CommandSystem.Register("BlockUse", AccessLevel.Administrator, BlockUse_OnCommand);
        CommandSystem.Register("UnblockUse", AccessLevel.Administrator, UnblockUse_OnCommand);
        CommandSystem.Register("BlockSkill", AccessLevel.Administrator, BlockSkill_OnCommand);
        CommandSystem.Register("UnblockSkill", AccessLevel.Administrator, UnblockSkill_OnCommand);
        CommandSystem.Register("BlockSpell", AccessLevel.Administrator, BlockSpell_OnCommand);
        CommandSystem.Register("UnblockSpell", AccessLevel.Administrator, UnblockSpell_OnCommand);
        CommandSystem.Register("BlockContainer", AccessLevel.Administrator, BlockContainer_OnCommand);
        CommandSystem.Register("UnblockContainer", AccessLevel.Administrator, UnblockContainer_OnCommand);
        CommandSystem.Register("FeatureList", AccessLevel.GameMaster, FeatureList_OnCommand);
        CommandSystem.Register("FeatureAdmin", AccessLevel.Administrator, FeatureAdmin_OnCommand);
        CommandSystem.Register("ListGumps", AccessLevel.GameMaster, ListGumps_OnCommand);
    }

    [Usage("FeatureFlag <flagKey> [on|off|toggle|info|create|delete]")]
    [Aliases("FF")]
    [Description("Manage feature flags. Use without arguments to open admin gump.")]
    private static void FeatureFlag_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendGump(new FeatureFlagAdminGump());
            return;
        }

        var flagKey = e.Arguments[0].ToLowerInvariant();
        var action = e.Arguments.Length > 1 ? e.Arguments[1].ToLowerInvariant() : "info";

        if (action is "on" or "enable" or "true" or "1")
        {
            if (FeatureFlagManager.SetFlag(flagKey, true, from.Name))
            {
                from.SendMessage(0x35, $"Feature flag '{flagKey}' has been ENABLED.");
            }
            else
            {
                from.SendMessage(0x22, $"Feature flag '{flagKey}' not found.");
            }
        }
        else if (action is "off" or "disable" or "false" or "0")
        {
            if (FeatureFlagManager.SetFlag(flagKey, false, from.Name))
            {
                from.SendMessage(0x35, $"Feature flag '{flagKey}' has been DISABLED.");
            }
            else
            {
                from.SendMessage(0x22, $"Feature flag '{flagKey}' not found.");
            }
        }
        else if (action == "toggle")
        {
            var flag = FeatureFlagManager.GetFlag(flagKey);
            if (flag != null)
            {
                FeatureFlagManager.SetFlag(flagKey, !flag.Enabled, from.Name);
                from.SendMessage(0x35, $"Feature flag '{flagKey}' toggled to {(flag.Enabled ? "DISABLED" : "ENABLED")}.");
            }
            else
            {
                from.SendMessage(0x22, $"Feature flag '{flagKey}' not found.");
            }
        }
        else if (action == "create")
        {
            if (e.Arguments.Length < 4)
            {
                from.SendMessage("Usage: [FeatureFlag <key> create <category> <description>");
                return;
            }

            var category = e.Arguments[2];
            var description = string.Join(" ", e.Arguments, 3, e.Arguments.Length - 3);
            FeatureFlagManager.CreateOrUpdateFlag(flagKey, description, category, true, from.Name);
            from.SendMessage(0x35, $"Feature flag '{flagKey}' created.");
        }
        else if (action is "delete" or "remove")
        {
            if (FeatureFlagManager.RemoveFlag(flagKey, from.Name))
            {
                from.SendMessage(0x35, $"Feature flag '{flagKey}' removed.");
            }
            else
            {
                from.SendMessage(0x22, $"Feature flag '{flagKey}' not found.");
            }
        }
        else
        {
            var infoFlag = FeatureFlagManager.GetFlag(flagKey);
            if (infoFlag != null)
            {
                from.SendMessage(0x35, $"=== Feature Flag: {infoFlag.Key} ===");
                from.SendMessage($"Enabled: {(infoFlag.Enabled ? "Yes" : "No")}");
                from.SendMessage($"Default: {(infoFlag.DefaultEnabled ? "Yes" : "No")}");
                from.SendMessage($"Category: {infoFlag.Category}");
                from.SendMessage($"Description: {infoFlag.Description}");
                from.SendMessage($"Last Modified: {infoFlag.LastModified:G} by {infoFlag.LastModifiedBy}");
            }
            else
            {
                from.SendMessage(0x22, $"Feature flag '{flagKey}' not found.");
                from.SendMessage("Use [FeatureList to see all available flags.");
            }
        }
    }

    [Usage("BlockGump <typeName> [reason]")]
    [Description("Block a gump type from being displayed to players.")]
    private static void BlockGump_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [BlockGump <typeName> [reason]");
            from.SendMessage("Example: [BlockGump CraftGump Crafting temporarily disabled");
            return;
        }

        var typeName = e.Arguments[0];
        var reason = e.Arguments.Length > 1
            ? string.Join(" ", e.Arguments, 1, e.Arguments.Length - 1)
            : "This feature is temporarily disabled.";

        if (FeatureFlagManager.BlockGumpByName(typeName, reason, from.Name))
        {
            from.SendMessage(0x35, $"Gump '{typeName}' has been BLOCKED.");
            from.SendMessage($"Reason: {reason}");
        }
        else
        {
            from.SendMessage(0x22, $"Could not find gump type '{typeName}'.");
            from.SendMessage("Make sure you're using the correct type name (e.g., CraftGump, HelpGump, etc.)");
        }
    }

    [Usage("UnblockGump <typeName>")]
    [Description("Remove a gump type block, allowing it to be displayed again.")]
    private static void UnblockGump_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [UnblockGump <typeName>");
            from.SendMessage("Use [FeatureList gumps to see blocked gumps.");
            return;
        }

        var typeName = e.Arguments[0];

        if (FeatureFlagManager.UnblockGumpByName(typeName, from.Name))
        {
            from.SendMessage(0x35, $"Gump block for '{typeName}' has been REMOVED.");
        }
        else
        {
            from.SendMessage(0x22, $"No block found for gump '{typeName}'.");
        }
    }

    [Usage("BlockUse <typeName> [reason]")]
    [Description("Block an item type's double-click usage.")]
    private static void BlockUse_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [BlockUse <typeName> [reason]");
            from.SendMessage("Example: [BlockUse BankCheck Dupe bug discovered");
            from.SendMessage("Or target an item: [BlockUse target [reason]");
            return;
        }

        if (e.Arguments[0].Equals("target", StringComparison.OrdinalIgnoreCase))
        {
            var reason = e.Arguments.Length > 1
                ? string.Join(" ", e.Arguments, 1, e.Arguments.Length - 1)
                : "This item cannot be used at this time.";

            from.SendMessage("Target the item to block its usage:");
            from.Target = new BlockUseTarget(reason);
            return;
        }

        var typeName = e.Arguments[0];
        var blockReason = e.Arguments.Length > 1
            ? string.Join(" ", e.Arguments, 1, e.Arguments.Length - 1)
            : "This item cannot be used at this time.";

        if (FeatureFlagManager.BlockUseReqByName(typeName, blockReason, from.Name))
        {
            from.SendMessage(0x35, $"Item usage for '{typeName}' has been BLOCKED.");
            from.SendMessage($"Reason: {blockReason}");
        }
        else
        {
            from.SendMessage(0x22, $"Could not find item type '{typeName}'.");
            from.SendMessage("Make sure you're using the correct type name (e.g., BankCheck, Gold, Runebook, etc.)");
        }
    }

    private sealed class BlockUseTarget : Target
    {
        private readonly string _reason;

        public BlockUseTarget(string reason) : base(-1, false, TargetFlags.None) => _reason = reason;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Item item)
            {
                var type = item.GetType();
                FeatureFlagManager.BlockUseReq(type, _reason, from.Name);
                from.SendMessage(0x35, $"Item usage for '{type.Name}' has been BLOCKED.");
                from.SendMessage($"Reason: {_reason}");
            }
            else
            {
                from.SendMessage(0x22, "That is not an item.");
            }
        }
    }

    [Usage("UnblockUse <typeName>")]
    [Description("Remove an item type's usage block.")]
    private static void UnblockUse_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [UnblockUse <typeName>");
            from.SendMessage("Use [FeatureList items to see blocked items.");
            return;
        }

        var typeName = e.Arguments[0];

        if (FeatureFlagManager.UnblockUseReqByName(typeName, from.Name))
        {
            from.SendMessage(0x35, $"Item usage block for '{typeName}' has been REMOVED.");
        }
        else
        {
            from.SendMessage(0x22, $"No block found for item '{typeName}'.");
        }
    }

    [Usage("BlockSkill <SkillName> [reason]")]
    [Description("Block a skill from being used by players.")]
    private static void BlockSkill_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [BlockSkill <SkillName> [reason]");
            from.SendMessage("Example: [BlockSkill Magery Investigating exploit");
            return;
        }

        if (!Enum.TryParse<SkillName>(e.Arguments[0], true, out var skill))
        {
            from.SendMessage(0x22, $"Unknown skill name '{e.Arguments[0]}'.");
            from.SendMessage("Valid skills: Alchemy, Anatomy, Magery, Mining, etc.");
            return;
        }

        var reason = e.Arguments.Length > 1
            ? string.Join(" ", e.Arguments, 1, e.Arguments.Length - 1)
            : "This skill is temporarily disabled.";

        FeatureFlagManager.BlockSkill(skill, reason, from.Name);
        from.SendMessage(0x35, $"Skill '{skill}' has been BLOCKED.");
        from.SendMessage($"Reason: {reason}");
    }

    [Usage("UnblockSkill <SkillName>")]
    [Description("Remove a skill block.")]
    private static void UnblockSkill_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [UnblockSkill <SkillName>");
            from.SendMessage("Use [FeatureList skills to see blocked skills.");
            return;
        }

        if (!Enum.TryParse<SkillName>(e.Arguments[0], true, out var skill))
        {
            from.SendMessage(0x22, $"Unknown skill name '{e.Arguments[0]}'.");
            return;
        }

        if (FeatureFlagManager.UnblockSkill(skill, from.Name))
        {
            from.SendMessage(0x35, $"Skill block for '{skill}' has been REMOVED.");
        }
        else
        {
            from.SendMessage(0x22, $"No block found for skill '{skill}'.");
        }
    }

    [Usage("BlockSpell <SpellTypeName> [reason]")]
    [Description("Block a spell from being cast by players.")]
    private static void BlockSpell_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [BlockSpell <SpellTypeName> [reason]");
            from.SendMessage("Example: [BlockSpell RecallSpell Investigating exploit");
            return;
        }

        var typeName = e.Arguments[0];
        var reason = e.Arguments.Length > 1
            ? string.Join(" ", e.Arguments, 1, e.Arguments.Length - 1)
            : "This spell is temporarily disabled.";

        if (FeatureFlagManager.BlockSpellByName(typeName, reason, from.Name))
        {
            from.SendMessage(0x35, $"Spell '{typeName}' has been BLOCKED.");
            from.SendMessage($"Reason: {reason}");
        }
        else
        {
            from.SendMessage(0x22, $"Could not find spell type '{typeName}'.");
            from.SendMessage("Make sure you're using the correct type name (e.g., RecallSpell, GateTravelSpell, etc.)");
        }
    }

    [Usage("UnblockSpell <SpellTypeName>")]
    [Description("Remove a spell block.")]
    private static void UnblockSpell_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [UnblockSpell <SpellTypeName>");
            from.SendMessage("Use [FeatureList spells to see blocked spells.");
            return;
        }

        var typeName = e.Arguments[0];

        if (FeatureFlagManager.UnblockSpellByName(typeName, from.Name))
        {
            from.SendMessage(0x35, $"Spell block for '{typeName}' has been REMOVED.");
        }
        else
        {
            from.SendMessage(0x22, $"No block found for spell '{typeName}'.");
        }
    }

    [Usage("BlockContainer <typeName> [reason]")]
    [Description("Block a container type from being opened by players.")]
    private static void BlockContainer_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [BlockContainer <typeName> [reason]");
            from.SendMessage("Example: [BlockContainer BankBox Bank access temporarily disabled");
            return;
        }

        var typeName = e.Arguments[0];
        var reason = e.Arguments.Length > 1
            ? string.Join(" ", e.Arguments, 1, e.Arguments.Length - 1)
            : FeatureFlagSettings.DefaultContainerBlockedMessage;

        if (FeatureFlagManager.BlockContainerByName(typeName, reason, from.Name))
        {
            from.SendMessage(0x35, $"Container '{typeName}' has been BLOCKED.");
            from.SendMessage($"Reason: {reason}");
        }
        else
        {
            from.SendMessage(0x22, $"Could not find container type '{typeName}'.");
            from.SendMessage("Make sure you're using the correct type name (e.g., BankBox, MetalChest, etc.)");
        }
    }

    [Usage("UnblockContainer <typeName>")]
    [Description("Remove a container type block.")]
    private static void UnblockContainer_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length == 0)
        {
            from.SendMessage("Usage: [UnblockContainer <typeName>");
            from.SendMessage("Use [FeatureList containers to see blocked containers.");
            return;
        }

        var typeName = e.Arguments[0];

        if (FeatureFlagManager.UnblockContainerByName(typeName, from.Name))
        {
            from.SendMessage(0x35, $"Container block for '{typeName}' has been REMOVED.");
        }
        else
        {
            from.SendMessage(0x22, $"No block found for container '{typeName}'.");
        }
    }

    [Usage("FeatureList [flags|gumps|items|skills|spells|containers|all]")]
    [Description("List all feature flags and blocks.")]
    private static void FeatureList_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var filter = e.Arguments.Length > 0 ? e.Arguments[0].ToLowerInvariant() : "all";

        if (filter is "flags" or "all")
        {
            var flags = new List<FeatureFlag>(FeatureFlagManager.GetAllFlags());
            flags.Sort((a, b) =>
            {
                var cmp = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
                return cmp != 0 ? cmp : string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
            });
            from.SendMessage(0x35, $"=== Feature Flags ({flags.Count}) ===");
            foreach (var flag in flags)
            {
                var status = flag.Enabled ? "[ON]" : "[OFF]";
                from.SendMessage($"  {status} {flag.Key} ({flag.Category}): {flag.Description}");
            }
        }

        if (filter is "gumps" or "all")
        {
            var gumpBlocks = FeatureFlagManager.GetAllGumpBlocks();
            from.SendMessage(0x35, $"=== Blocked Gumps ({gumpBlocks.Count}) ===");
            foreach (var block in gumpBlocks)
            {
                var status = block.Active ? "[OFF]" : "[ON]";
                from.SendMessage($"  {status} {block.DisplayName}: {block.Reason}");
            }
        }

        if (filter is "items" or "all")
        {
            var useReqBlocks = FeatureFlagManager.GetAllUseReqBlocks();
            from.SendMessage(0x35, $"=== Blocked Item Usage ({useReqBlocks.Count}) ===");
            foreach (var block in useReqBlocks)
            {
                var status = block.Active ? "[OFF]" : "[ON]";
                from.SendMessage($"  {status} {block.DisplayName}: {block.Reason}");
            }
        }

        if (filter is "skills" or "all")
        {
            var skillBlocks = FeatureFlagManager.GetAllSkillBlocks();
            from.SendMessage(0x35, $"=== Blocked Skills ({skillBlocks.Length}) ===");
            for (var i = 0; i < skillBlocks.Length; i++)
            {
                var block = skillBlocks[i];
                var status = block.Active ? "[OFF]" : "[ON]";
                from.SendMessage($"  {status} {block.DisplayName}: {block.Reason}");
            }
        }

        if (filter is "spells" or "all")
        {
            var spellBlocks = FeatureFlagManager.GetAllSpellBlocks();
            from.SendMessage(0x35, $"=== Blocked Spells ({spellBlocks.Length}) ===");
            foreach (var block in spellBlocks)
            {
                var status = block.Active ? "[OFF]" : "[ON]";
                from.SendMessage($"  {status} {block.DisplayName}: {block.Reason}");
            }
        }

        if (filter is "containers" or "all")
        {
            var containerBlocks = FeatureFlagManager.GetAllContainerBlocks();
            from.SendMessage(0x35, $"=== Blocked Containers ({containerBlocks.Count}) ===");
            foreach (var block in containerBlocks)
            {
                var status = block.Active ? "[OFF]" : "[ON]";
                from.SendMessage($"  {status} {block.DisplayName}: {block.Reason}");
            }
        }

        if (filter != "flags" && filter != "gumps" && filter != "items" && filter != "skills" && filter != "spells" && filter != "containers" && filter != "all")
        {
            from.SendMessage("Usage: [FeatureList [flags|gumps|items|skills|spells|containers|all]");
        }
    }

    [Usage("FeatureAdmin")]
    [Description("Open the feature flag administration gump.")]
    private static void FeatureAdmin_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new FeatureFlagAdminGump());
    }

    [Usage("ListGumps")]
    [Description("List all open gumps for yourself or a targeted player. Useful for finding gump names to block.")]
    private static void ListGumps_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;

        if (e.Arguments.Length > 0 && e.Arguments[0].Equals("self", StringComparison.OrdinalIgnoreCase))
        {
            ListGumpsFor(from, from);
        }
        else
        {
            from.SendMessage("Target a player to list their open gumps (or use [ListGumps self):");
            from.Target = new ListGumpsTarget();
        }
    }

    private static void ListGumpsFor(Mobile from, Mobile target)
    {
        if (target?.NetState == null)
        {
            from.SendMessage(0x22, "That player is not online.");
            return;
        }

        var gumps = target.GetGumps();
        var count = 0;
        var gumpList = new List<(string shortName, string fullName)>();

        foreach (var gump in gumps)
        {
            var type = gump.GetType();
            var fullName = type.FullName ?? type.Name;
            var shortName = type.Name;
            gumpList.Add((shortName, fullName));
            count++;
        }

        if (count == 0)
        {
            from.SendMessage(0x35, $"{target.Name} has no gumps open.");
            return;
        }

        from.SendMessage(0x35, $"=== Open Gumps for {target.Name} ({count}) ===");
        foreach (var (shortName, fullName) in gumpList)
        {
            from.SendMessage($"  • {shortName}");
            from.SendMessage($"      Full: {fullName}");
            from.SendMessage(0x3B2, $"      Block with: [BlockGump {shortName}");
        }
    }

    private sealed class ListGumpsTarget : Target
    {
        public ListGumpsTarget() : base(-1, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Mobile m)
            {
                ListGumpsFor(from, m);
            }
            else
            {
                from.SendMessage(0x22, "That is not a player.");
            }
        }
    }
}
