using System;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.Engines.Craft.T2A;

public class BowFletchingMenu : ItemListMenu
{
    private static readonly Type[] ItemTypes =
    [
        typeof(Kindling), typeof(Shaft), typeof(Arrow), typeof(Bolt),
        typeof(Bow), typeof(Crossbow), typeof(HeavyCrossbow)
    ];

    private static readonly string[] ItemNames =
    [
        "Kindling", "Shafts", "Arrows", "Bolts", "Bow", "Crossbow", "Heavy Crossbow"
    ];

    private static readonly int[] ItemGraphics = [0xDE1, 0x1BD4, 0xF3F, 0x1BFB, 0x13B2, 0xF50, 0x13FD];

    private static ItemListEntry[] _cachedEntries;

    private readonly BaseTool _tool;

    public BowFletchingMenu(Mobile from, BaseTool tool)
        : base("What would you like to make?", BuildFilteredEntries(from))
    {
        _tool = tool;
    }

    public static ItemListEntry[] Main()
    {
        if (_cachedEntries != null)
        {
            return _cachedEntries;
        }

        var entries = new ItemListEntry[ItemTypes.Length];
        var count = 0;
        var craftItems = DefBowFletching.CraftSystem.CraftItems;

        for (var i = 0; i < ItemTypes.Length; i++)
        {
            if (craftItems.SearchFor(ItemTypes[i]) != null)
            {
                entries[count++] = new ItemListEntry(ItemNames[i], ItemGraphics[i], 0, i);
            }
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
        return T2ACraftSystem.FilterEntries(from, Main(), ItemTypes, DefBowFletching.CraftSystem);
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = Entries[index].CraftIndex;

        if (craftIndex < 0 || craftIndex >= ItemTypes.Length)
        {
            return;
        }

        var itemDef = DefBowFletching.CraftSystem.CraftItems.SearchFor(ItemTypes[craftIndex]);
        if (itemDef == null)
        {
            return;
        }

        var num = DefBowFletching.CraftSystem.CanCraft(from, _tool, itemDef.ItemType);
        if (num > 0)
        {
            from.SendLocalizedMessage(num);
            return;
        }

        var context = DefBowFletching.CraftSystem.GetContext(from);
        var res = itemDef.UseSubRes2 ? DefBowFletching.CraftSystem.CraftSubRes2 : DefBowFletching.CraftSystem.CraftSubRes;
        var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        var type = resIndex > -1 ? res[resIndex].ItemType : null;
        DefBowFletching.CraftSystem.CreateItem(from, itemDef.ItemType, type, _tool, itemDef);
    }
}
