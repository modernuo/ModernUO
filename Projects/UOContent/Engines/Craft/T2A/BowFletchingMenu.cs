using System;
using Server.Items;
using Server.Network;
using Server.Menus.ItemLists;
using System.Collections.Generic;

namespace Server.Engines.Craft.T2A;

public class BowFletchingMenu : ItemListMenu
{
    private readonly Mobile m_Mobile;
    private readonly string m_IsFrom;
    private readonly BaseTool m_Tool;
    private readonly ItemListEntry[] m_Entries;

    public BowFletchingMenu(Mobile m, ItemListEntry[] entries, string isFrom, BaseTool tool)
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
        var craftItems = DefBowFletching.CraftSystem.CraftItems;
        bool allRequiredSkills;
        double chance;
        int idx;

        // Kindling
        idx = craftItems.FindIndex(ci => ci.ItemType == typeof(Kindling));
        if (idx >= 0)
        {
            var itemDef = craftItems[idx];
            chance = itemDef.GetSuccessChance(from, typeof(Log), DefBowFletching.CraftSystem, false, out allRequiredSkills);
            int resAmount = from.Backpack?.GetAmount(typeof(Log)) ?? 0;
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                entries.Add(new ItemListEntry("Kindling", 0xDE1, 0, idx));
            }
        }

        // Shafts
        idx = craftItems.FindIndex(ci => ci.ItemType == typeof(Shaft));
        if (idx >= 0)
        {
            var itemDef = craftItems[idx];
            chance = itemDef.GetSuccessChance(from, typeof(Log), DefBowFletching.CraftSystem, false, out allRequiredSkills);
            int resAmount = from.Backpack?.GetAmount(typeof(Log)) ?? 0;
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                entries.Add(new ItemListEntry("Shafts", 0x1BD4, 0, idx));
            }
        }

        // Arrows
        idx = craftItems.FindIndex(ci => ci.ItemType == typeof(Arrow));
        if (idx >= 0)
        {
            var itemDef = craftItems[idx];
            chance = itemDef.GetSuccessChance(from, typeof(Shaft), DefBowFletching.CraftSystem, false, out allRequiredSkills);
            int resAmount = from.Backpack?.GetAmount(typeof(Shaft)) ?? 0;
            int featherAmount = from.Backpack?.GetAmount(typeof(Feather)) ?? 0;
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount && featherAmount >= itemDef.Resources[1].Amount)
            {
                entries.Add(new ItemListEntry("Arrows", 0xF3F, 0, idx));
            }
        }

        // Bolts
        idx = craftItems.FindIndex(ci => ci.ItemType == typeof(Bolt));
        if (idx >= 0)
        {
            var itemDef = craftItems[idx];
            chance = itemDef.GetSuccessChance(from, typeof(Shaft), DefBowFletching.CraftSystem, false, out allRequiredSkills);
            int resAmount = from.Backpack?.GetAmount(typeof(Shaft)) ?? 0;
            int featherAmount = from.Backpack?.GetAmount(typeof(Feather)) ?? 0;
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount && featherAmount >= itemDef.Resources[1].Amount)
            {
                entries.Add(new ItemListEntry("Bolts", 0x1BFB, 0, idx));
            }
        }

        // Bow
        idx = craftItems.FindIndex(ci => ci.ItemType == typeof(Bow));
        if (idx >= 0)
        {
            var itemDef = craftItems[idx];
            chance = itemDef.GetSuccessChance(from, typeof(Log), DefBowFletching.CraftSystem, false, out allRequiredSkills);
            int resAmount = from.Backpack?.GetAmount(typeof(Log)) ?? 0;
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                entries.Add(new ItemListEntry("Bow", 0x13B2, 0, idx));
            }
        }

        // Crossbow
        idx = craftItems.FindIndex(ci => ci.ItemType == typeof(Crossbow));
        if (idx >= 0)
        {
            var itemDef = craftItems[idx];
            chance = itemDef.GetSuccessChance(from, typeof(Log), DefBowFletching.CraftSystem, false, out allRequiredSkills);
            int resAmount = from.Backpack?.GetAmount(typeof(Log)) ?? 0;
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                entries.Add(new ItemListEntry("Crossbow", 0xF50, 0, idx));
            }
        }

        // Heavy Crossbow
        idx = craftItems.FindIndex(ci => ci.ItemType == typeof(HeavyCrossbow));
        if (idx >= 0)
        {
            var itemDef = craftItems[idx];
            chance = itemDef.GetSuccessChance(from, typeof(Log), DefBowFletching.CraftSystem, false, out allRequiredSkills);
            int resAmount = from.Backpack?.GetAmount(typeof(Log)) ?? 0;
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                entries.Add(new ItemListEntry("Heavy Crossbow", 0x13FD, 0, idx));
            }
        }

        return entries.ToArray();
    }

    public override void OnResponse(NetState state, int index)
    {
        if (index < 0 || index >= m_Entries.Length)
        {
            m_Mobile.SendAsciiMessage("Invalid selection.");
            m_Mobile.SendMenu(new BowFletchingMenu(m_Mobile, Main(m_Mobile), "Main", m_Tool));
            return;
        }
        var itemDef = DefBowFletching.CraftSystem.CraftItems[m_Entries[index].CraftIndex];
        int num = DefBowFletching.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
        if (num > 0)
        {
            m_Mobile.SendAsciiMessage("You don't have the required skills to attempt this item.");
            m_Mobile.SendMenu(new BowFletchingMenu(m_Mobile, Main(m_Mobile), "Main", m_Tool));
            return;
        }
        var context = DefBowFletching.CraftSystem.GetContext(m_Mobile);
        var res = itemDef.UseSubRes2 ? DefBowFletching.CraftSystem.CraftSubRes2 : DefBowFletching.CraftSystem.CraftSubRes;
        int resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        Type type = resIndex > -1 ? res[resIndex].ItemType : null;
        DefBowFletching.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, type, m_Tool, itemDef);
    }
}

public static class UserBowFletchingMenu
{
    public static ItemListEntry[] Main(Mobile from)
    {
        var entries = new ItemListEntry[DefBowFletching.CraftSystem.CraftItems.Count];
        int missing = 0;
        for (int i = 0; i < DefBowFletching.CraftSystem.CraftItems.Count; ++i)
        {
            if (DefBowFletching.CraftSystem.CraftItems[i].Resources.Count == 0)
            {
                missing++;
                continue;
            }
            var itemDef = DefBowFletching.CraftSystem.CraftItems[i];
            bool allRequiredSkills = true;
            double chance = itemDef.GetSuccessChance(from, typeof(Log), DefBowFletching.CraftSystem, false, out allRequiredSkills);
            if (chance > 0)
            {
                var type = itemDef.ItemType;
                var res = itemDef.Resources.Count > 0 ? itemDef.Resources[0] : null;
                int resAmount = from.Backpack.GetAmount(typeof(Log));
                Item item = null;
                try { item = Activator.CreateInstance(type) as Item; } catch { }
                var name = item?.GetType().Name ?? "";
                name = name.Replace("yC", "y C").ToLower();
                var itemid = item?.ItemID ?? 0;
                // Shafts
                if (i == 1)
                {
                    if (resAmount != 0)
                    {
                        entries[i-missing] = new ItemListEntry("arrow shafts using all wood", itemid, 0, i);
                    }
                    else
                    {
                        missing++;
                    }
                }
                // Arrows, bolts
                else if (i == 2 || i == 3)
                {
                    missing++;
                }
                // Kindling and bows
                else
                {
                    if (resAmount != 0 && res != null && resAmount >= res.Amount)
                    {
                        entries[i-missing] = new ItemListEntry(name, itemid, 0, i);
                    }
                    else
                    {
                        missing++;
                    }
                }
                item?.Delete();
            }
            else
            {
                missing++;
            }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static void OnResponse(Mobile m_Mobile, string IsFrom, ItemListEntry[] m_Entries, int index, BaseTool m_Tool)
    {
        var itemDef = DefBowFletching.CraftSystem.CraftItems[m_Entries[index].CraftIndex];
        int num = DefBowFletching.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
        if (num > 0)
        {
            m_Mobile.SendAsciiMessage("You don't have the required skills to attempt this item.");
            m_Mobile.SendMenu(new BowFletchingMenu(m_Mobile, BowFletchingMenu.Main(m_Mobile), "Main", m_Tool));
            return;
        }
        var context = DefBowFletching.CraftSystem.GetContext(m_Mobile);
        var res = itemDef.UseSubRes2 ? DefBowFletching.CraftSystem.CraftSubRes2 : DefBowFletching.CraftSystem.CraftSubRes;
        int resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        Type type = resIndex > -1 ? res[resIndex].ItemType : null;
        DefBowFletching.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, type, m_Tool, itemDef);
    }
} 