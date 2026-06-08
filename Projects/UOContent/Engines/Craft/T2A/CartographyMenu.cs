using System;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.Engines.Craft.T2A;

public class CartographyMenu : ItemListMenu
{
    private static ItemListEntry[] _cachedEntries;

    private readonly BaseTool _tool;

    public CartographyMenu(Mobile from, BaseTool tool)
        : base("What kind of map?", BuildFilteredEntries(from))
    {
        _tool = tool;
    }

    public static ItemListEntry[] Main()
    {
        if (_cachedEntries != null)
        {
            return _cachedEntries;
        }

        var craftItems = DefCartography.CraftSystem.CraftItems;
        var entries = new ItemListEntry[craftItems.Count];
        var count = 0;

        for (var i = 0; i < craftItems.Count; i++)
        {
            var name = i switch
            {
                0 => "A map of the local environs.",
                1 => "A map suitable for cities.",
                2 => "A moderately sized sea chart.",
                3 => "A map of the world.",
                _ => craftItems[i].ItemType.Name
            };

            entries[count++] = new ItemListEntry(name, 6511 + i, 0, i);
        }

        if (count < entries.Length)
        {
            Array.Resize(ref entries, count);
        }

        _cachedEntries = entries;
        return entries;
    }

    private static ItemListEntry[] BuildFilteredEntries(Mobile from)
    {
        var staticEntries = Main();
        var craftItems = DefCartography.CraftSystem.CraftItems;

        var filtered = new ItemListEntry[staticEntries.Length];
        var count = 0;

        for (var i = 0; i < staticEntries.Length; i++)
        {
            var entry = staticEntries[i];
            var craftIndex = entry.CraftIndex;
            if (craftIndex >= 0 && craftIndex < craftItems.Count)
            {
                var itemDef = craftItems[craftIndex];
                if (T2ACraftSystem.CanCraftItem(from, itemDef, DefCartography.CraftSystem))
                {
                    filtered[count++] = entry;
                }
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

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = Entries[index].CraftIndex;
        var craftItems = DefCartography.CraftSystem.CraftItems;

        if (craftIndex < 0 || craftIndex >= craftItems.Count)
        {
            return;
        }

        var itemDef = craftItems[craftIndex];
        var num = DefCartography.CraftSystem.CanCraft(from, _tool, itemDef.ItemType);
        if (num > 0)
        {
            from.SendLocalizedMessage(num);
            return;
        }

        DefCartography.CraftSystem.CreateItem(from, itemDef.ItemType, typeof(BlankMap), _tool, itemDef);
    }
}
