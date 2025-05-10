using System;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.Engines.Craft.T2A;

public class CartographyMenu : ItemListMenu
{
    private static ItemListEntry[] _cachedEntries;

    private readonly BaseTool _tool;
    private readonly ItemListEntry[] _entries;

    public CartographyMenu(BaseTool tool, ItemListEntry[] entries = null)
        : base("Choose an item.", entries ??= Main())
    {
        _tool = tool;
        _entries = entries;
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

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = _entries[index].CraftIndex;
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
