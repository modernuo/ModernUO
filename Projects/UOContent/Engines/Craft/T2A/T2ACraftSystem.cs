// T2A crafting system: packet-based, not gump-based

using System;
using Server.Items;
using Server.Menus.ItemLists;

namespace Server.Engines.Craft.T2A;

public static class T2ACraftSystem
{
    public static void ShowMenu(Mobile from, CraftSystem craftSystem, BaseTool tool)
    {
        if (Core.UOTD)
        {
            return;
        }

        if (craftSystem == DefBlacksmithy.CraftSystem)
        {
            // Lost Lands flow: resource selection first, then menu
            BlacksmithMenu.ResourceSelection(from, tool, (mob, t) =>
            {
                var menu = new BlacksmithMenu(mob, t);
                if (menu.Entries.Length == 0)
                {
                    mob.SendAsciiMessage("You lack the skill and materials to craft anything.");
                    return;
                }

                mob.SendMenu(menu);
            });
        }
        else if (craftSystem == DefAlchemy.CraftSystem)
        {
            var menu = new AlchemyMenu(from, tool);
            if (menu.Entries.Length == 0)
            {
                from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                return;
            }

            from.SendMenu(menu);
        }
        else if (craftSystem == DefBowFletching.CraftSystem)
        {
            var menu = new BowFletchingMenu(from, tool);
            if (menu.Entries.Length == 0)
            {
                from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                return;
            }

            from.SendMenu(menu);
        }
        else if (craftSystem == DefCarpentry.CraftSystem)
        {
            var menu = new CarpentryMenu(from, tool);
            if (menu.Entries.Length == 0)
            {
                from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                return;
            }

            from.SendMenu(menu);
        }
        else if (craftSystem == DefCartography.CraftSystem)
        {
            var menu = new CartographyMenu(from, tool);
            if (menu.Entries.Length == 0)
            {
                from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                return;
            }

            from.SendMenu(menu);
        }
        else if (craftSystem == DefInscription.CraftSystem)
        {
            var menu = new InscriptionMenu(from, tool);
            if (menu.Entries.Length == 0)
            {
                from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                return;
            }

            from.SendMenu(menu);
        }
        else if (craftSystem == DefTailoring.CraftSystem)
        {
            TailoringMenu.ResourceSelection(from, tool);
        }
        else if (craftSystem == DefTinkering.CraftSystem)
        {
            TinkeringMenu.ResourceSelection(from, tool);
        }
    }

    /// <summary>
    /// Checks if a player can craft a specific item, accounting for sub-resource types.
    /// When <paramref name="selectedResourceType"/> is non-null, checks against that specific sub-resource.
    /// When null, checks against ANY available sub-resource the player has sufficient skill and materials for.
    /// </summary>
    public static bool CanCraftItem(
        Mobile from, CraftItem itemDef, CraftSystem system, Type selectedResourceType = null
    )
    {
        var pack = from.Backpack;
        if (pack == null)
        {
            return false;
        }

        var chance = itemDef.GetSuccessChance(from, selectedResourceType, system, false, out var allRequiredSkills);
        if (!allRequiredSkills || chance <= 0.0)
        {
            return false;
        }

        var resCol = system.CraftSubRes;

        for (var i = 0; i < itemDef.Resources.Count; i++)
        {
            var res = itemDef.Resources[i];
            var resType = res.ItemType;

            // If this resource is the base sub-resource type (e.g. IronIngot for blacksmithing),
            // handle sub-resource substitution
            if (resCol.Init && resType == resCol.ResType)
            {
                if (selectedResourceType != null)
                {
                    // Specific resource selected — check skill gate for this sub-resource
                    var subRes = resCol.SearchFor(selectedResourceType);
                    if (subRes != null && from.Skills[system.MainSkill].Value < subRes.RequiredSkill)
                    {
                        return false;
                    }

                    // Check if player has enough of the selected resource
                    if (pack.GetAmount(selectedResourceType) < res.Amount)
                    {
                        return false;
                    }
                }
                else if (!HasAnySufficientSubResource(from, pack, res.Amount, system, resCol))
                {
                    return false;
                }
            }
            else if (pack.GetAmount(resType) < res.Amount)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Convenience overload that resolves a Type to a CraftItem first.
    /// </summary>
    public static bool CanCraftItem(Mobile from, Type itemType, CraftSystem system, Type selectedResourceType = null)
    {
        var itemDef = system.CraftItems.SearchFor(itemType);
        return itemDef != null && CanCraftItem(from, itemDef, system, selectedResourceType);
    }

    /// <summary>
    /// Filters static template entries to only those the player can craft.
    /// </summary>
    public static ItemListEntry[] FilterEntries(
        Mobile from, ItemListEntry[] staticEntries, Type[] types, CraftSystem system, Type selectedResourceType = null
    )
    {
        var filtered = new ItemListEntry[staticEntries.Length];
        var count = 0;

        for (var i = 0; i < staticEntries.Length; i++)
        {
            var entry = staticEntries[i];
            var typeIndex = entry.CraftIndex;
            if (typeIndex >= 0 && typeIndex < types.Length &&
                CanCraftItem(from, types[typeIndex], system, selectedResourceType))
            {
                filtered[count++] = entry;
            }
        }

        if (count == 0)
        {
            return [];
        }

        if (count < filtered.Length)
        {
            Array.Resize(ref filtered, count);
        }

        return filtered;
    }

    /// <summary>
    /// Returns true if at least one item in the type array is craftable.
    /// </summary>
    public static bool AnyCraftableInCategory(
        Mobile from, Type[] types, CraftSystem system, Type selectedResourceType = null
    )
    {
        for (var i = 0; i < types.Length; i++)
        {
            if (CanCraftItem(from, types[i], system, selectedResourceType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAnySufficientSubResource(
        Mobile from, Container pack, int amountNeeded, CraftSystem system, CraftSubResCol resCol
    )
    {
        for (var j = 0; j < resCol.Count; j++)
        {
            var subRes = resCol[j];
            if (from.Skills[system.MainSkill].Value >= subRes.RequiredSkill &&
                pack.GetAmount(subRes.ItemType) >= amountNeeded)
            {
                return true;
            }
        }

        return false;
    }
}
