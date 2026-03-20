// T2A crafting system: packet-based, not gump-based

using System;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Systems.FeatureFlags;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

public static class T2ACraftSystem
{
    public static void ShowMenu(Mobile from, CraftSystem craftSystem, BaseTool tool, Item preTarget = null)
    {
        if (!ContentFeatureFlags.T2ACraftMenus)
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
            }, preTarget);
        }
        else if (craftSystem == DefAlchemy.CraftSystem)
        {
            if (preTarget is BaseReagent or Bottle || preTarget == null)
            {
                ShowMenuDirect<AlchemyMenu>(from, tool);
            }
            else
            {
                PromptForResource(from, tool, craftSystem, "Target a reagent or empty bottle.",
                    item => item is BaseReagent or Bottle);
            }
        }
        else if (craftSystem == DefBowFletching.CraftSystem)
        {
            if (preTarget is Log or Board or Feather or Shaft || preTarget == null)
            {
                ShowMenuDirect<BowFletchingMenu>(from, tool);
            }
            else
            {
                PromptForResource(from, tool, craftSystem, "Target the wood or feathers you wish to use.",
                    item => item is Log or Board or Feather or Shaft);
            }
        }
        else if (craftSystem == DefCarpentry.CraftSystem)
        {
            if (preTarget is Log or Board || preTarget == null)
            {
                ShowMenuDirect<CarpentryMenu>(from, tool);
            }
            else
            {
                PromptForResource(from, tool, craftSystem, "Target the wood you wish to use.",
                    item => item is Log or Board);
            }
        }
        else if (craftSystem == DefCartography.CraftSystem)
        {
            if (preTarget is BlankMap || preTarget == null)
            {
                ShowMenuDirect<CartographyMenu>(from, tool);
            }
            else
            {
                PromptForResource(from, tool, craftSystem, "Target a blank map.",
                    item => item is BlankMap);
            }
        }
        else if (craftSystem == DefInscription.CraftSystem)
        {
            if (preTarget is BlankScroll or BaseReagent or RecallRune || preTarget == null)
            {
                ShowMenuDirect<InscriptionMenu>(from, tool);
            }
            else
            {
                PromptForResource(from, tool, craftSystem, "Target the blank scrolls you wish to use.",
                    item => item is BlankScroll or BaseReagent or RecallRune);
            }
        }
        else if (craftSystem == DefTailoring.CraftSystem)
        {
            TailoringMenu.ResourceSelection(from, tool, preTarget);
        }
        else if (craftSystem == DefTinkering.CraftSystem)
        {
            TinkeringMenu.ResourceSelection(from, tool, preTarget);
        }
    }

    private static void ShowMenuDirect<T>(Mobile from, BaseTool tool) where T : ItemListMenu
    {
        var menu = (T)Activator.CreateInstance(typeof(T), from, tool);
        if (menu.Entries.Length == 0)
        {
            from.SendAsciiMessage("You lack the skill and materials to craft anything.");
            return;
        }

        from.SendMenu(menu);
    }

    private static void PromptForResource(
        Mobile from, BaseTool tool, CraftSystem system, string message, Func<Item, bool> isValid
    )
    {
        from.SendAsciiMessage(message);
        from.Target = new CraftResourceTarget(tool, system, message, isValid);
    }

    private class CraftResourceTarget : Target
    {
        private readonly BaseTool _tool;
        private readonly CraftSystem _system;
        private readonly string _message;
        private readonly Func<Item, bool> _isValid;

        public CraftResourceTarget(
            BaseTool tool, CraftSystem system, string message, Func<Item, bool> isValid
        ) : base(12, false, TargetFlags.None)
        {
            _tool = tool;
            _system = system;
            _message = message;
            _isValid = isValid;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Item item && _isValid(item))
            {
                ShowMenu(from, _system, _tool, item);
                return;
            }

            from.SendAsciiMessage(_message);
            from.Target = new CraftResourceTarget(_tool, _system, _message, _isValid);
        }
    }

    /// <summary>
    /// Persists the selected resource type as a LastResourceIndex on the craft context,
    /// so that make-last can recall which resource was used.
    /// </summary>
    public static void SetLastResourceIndex(Mobile from, CraftSystem system, Type selectedResourceType)
    {
        if (selectedResourceType == null)
        {
            return;
        }

        var context = system.GetContext(from);
        var resCol = system.CraftSubRes;

        if (context == null || !resCol.Init)
        {
            return;
        }

        for (var i = 0; i < resCol.Count; i++)
        {
            if (resCol[i].ItemType == selectedResourceType)
            {
                context.LastResourceIndex = i;
                return;
            }
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
                    if (GetResourceAmount(pack, selectedResourceType) < res.Amount)
                    {
                        return false;
                    }
                }
                else if (!HasAnySufficientSubResource(from, pack, res.Amount, system, resCol))
                {
                    return false;
                }
            }
            else if (GetResourceAmount(pack, resType) < res.Amount)
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

    /// <summary>
    /// Equivalent type pairs mirroring CraftItem.m_TypesTable — used so that menu filtering
    /// counts boards when checking for logs, hides when checking for leather, etc.
    /// </summary>
    private static readonly Type[][] _equivalentTypes =
    [
        [typeof(Log), typeof(Board)],
        [typeof(Cloth), typeof(UncutCloth)],
        [typeof(Items.Leather), typeof(Hides)]
    ];

    private static int GetResourceAmount(Container pack, Type type)
    {
        for (var i = 0; i < _equivalentTypes.Length; i++)
        {
            if (_equivalentTypes[i][0] == type)
            {
                return pack.GetAmount(_equivalentTypes[i]);
            }
        }

        return pack.GetAmount(type);
    }

    private static bool HasAnySufficientSubResource(
        Mobile from, Container pack, int amountNeeded, CraftSystem system, CraftSubResCol resCol
    )
    {
        for (var j = 0; j < resCol.Count; j++)
        {
            var subRes = resCol[j];
            if (from.Skills[system.MainSkill].Value >= subRes.RequiredSkill &&
                GetResourceAmount(pack, subRes.ItemType) >= amountNeeded)
            {
                return true;
            }
        }

        return false;
    }
}
