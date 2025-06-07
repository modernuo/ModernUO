using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;
using Server.Menus.ItemLists;

namespace Server.Engines.Craft.T2A;

public class CartographyMenu : ItemListMenu
{
    private readonly Mobile m_Mobile;
    private readonly string m_IsFrom;
    private readonly BaseTool m_Tool;
    private readonly ItemListEntry[] m_Entries;

    public CartographyMenu(Mobile m, ItemListEntry[] entries, string isFrom, BaseTool tool)
        : base("Choose an item.", entries)
    {
        m_Mobile = m;
        m_IsFrom = isFrom;
        m_Tool = tool;
        m_Entries = entries;
    }

    public static ItemListEntry[] Main(Mobile from)
    {
        var entries = new List<ItemListEntry>();
        var craftItems = DefCartography.CraftSystem.CraftItems;
        bool allRequiredSkills;
        double chance;
        for (int i = 0; i < craftItems.Count; ++i)
        {
            var itemDef = craftItems[i];
            chance = itemDef.GetSuccessChance(from, typeof(BlankMap), DefCartography.CraftSystem, false, out allRequiredSkills);
            int resAmount = from.Backpack.GetAmount(typeof(BlankMap));
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                string name;
                switch (i)
                {
                    case 0: name = "A map of the local environs."; break;
                    case 1: name = "A map suitable for cities."; break;
                    case 2: name = "A moderately sized sea chart."; break;
                    case 3: name = "A map of the world."; break;
                    default: name = itemDef.ItemType.Name; break;
                }
                int itemid = 6511 + i; // Classic map item IDs
                entries.Add(new ItemListEntry(name, itemid, 0, i));
            }
        }
        return entries.Count > 0 ? entries.ToArray() : Array.Empty<ItemListEntry>();
    }

    public override void OnResponse(NetState state, int index)
    {
        if (index < 0 || index >= m_Entries.Length)
        {
            m_Mobile.SendAsciiMessage("Invalid selection.");
            m_Mobile.SendMenu(new CartographyMenu(m_Mobile, Main(m_Mobile), "Main", m_Tool));
            return;
        }
        var itemDef = DefCartography.CraftSystem.CraftItems[m_Entries[index].CraftIndex];
        int num = DefCartography.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
        if (num > 0)
        {
            m_Mobile.SendAsciiMessage("You don't have the required skills to attempt this item.");
            m_Mobile.SendMenu(new CartographyMenu(m_Mobile, Main(m_Mobile), "Main", m_Tool));
            return;
        }
        var context = DefCartography.CraftSystem.GetContext(m_Mobile);
        var res = itemDef.UseSubRes2 ? DefCartography.CraftSystem.CraftSubRes2 : DefCartography.CraftSystem.CraftSubRes;
        int resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        Type type = resIndex > -1 ? res[resIndex].ItemType : null;
        DefCartography.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, typeof(BlankMap), m_Tool, itemDef);
    }
} 