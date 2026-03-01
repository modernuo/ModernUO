using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;
using Server.Menus.ItemLists;

namespace Server.Engines.Craft.T2A;

public class CarpentryMenu : ItemListMenu
{
    private readonly Mobile m_Mobile;
    private readonly string m_IsFrom;
    private readonly BaseTool m_Tool;
    private readonly ItemListEntry[] m_Entries;

    public CarpentryMenu(Mobile m, ItemListEntry[] entries, string isFrom, BaseTool tool)
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
        var craftItems = DefCarpentry.CraftSystem.CraftItems;
        bool allRequiredSkills;
        double chance;
        int resAmount;

        // Chairs
        var chairType = craftItems[6].ItemType;
        var chairRes = craftItems[6].Resources[0];
        resAmount = from.Backpack.GetAmount(typeof(Log)) + from.Backpack.GetAmount(typeof(Board));
        chance = craftItems[6].GetSuccessChance(from, typeof(Board), DefCarpentry.CraftSystem, false, out allRequiredSkills);
        if (chance > 0 && resAmount >= chairRes.Amount)
        {
            entries.Add(new ItemListEntry("Chairs", 2902));
        }

        // Tables
        var tableType = craftItems[15].ItemType;
        var tableRes = craftItems[15].Resources[0];
        resAmount = from.Backpack.GetAmount(typeof(Log)) + from.Backpack.GetAmount(typeof(Board));
        chance = craftItems[15].GetSuccessChance(from, typeof(Board), DefCarpentry.CraftSystem, false, out allRequiredSkills);
        if (chance > 0 && resAmount >= tableRes.Amount)
        {
            entries.Add(new ItemListEntry("Tables", 2940, 0, 1));
        }

        // Miscellaneous (includes containers)
        var miscType = craftItems[21].ItemType;
        var miscRes = craftItems[21].Resources[0];
        resAmount = from.Backpack.GetAmount(typeof(Log)) + from.Backpack.GetAmount(typeof(Board));
        chance = craftItems[21].GetSuccessChance(from, typeof(Board), DefCarpentry.CraftSystem, false, out allRequiredSkills);
        if (chance > 0 && resAmount >= miscRes.Amount)
        {
            entries.Add(new ItemListEntry("Miscellaneous", 3650, 0, 2));
        }

        return entries.Count > 0 ? entries.ToArray() : Array.Empty<ItemListEntry>();
    }

    public static ItemListEntry[] Chairs(Mobile from)
    {
        var entries = new List<ItemListEntry>();
        var craftItems = DefCarpentry.CraftSystem.CraftItems;

        for (int i = 0; i < 9; ++i)
        {
            var itemDef = craftItems[i + 6];
            var chance = itemDef.GetSuccessChance(from, typeof(Board), DefCarpentry.CraftSystem, false, out _);
            var resAmount = from.Backpack.GetAmount(typeof(Log)) + from.Backpack.GetAmount(typeof(Board));

            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;

                var name = item?.GetType().Name ?? "";
                name = name.Replace("tS", "t S").Replace("oC", "o C").Replace("nC", "n C").Replace("nB", "n B").Replace("nT", "n T").ToLower();

                if (name == "fancywooden chaircushion" || name == "wooden chaircushion")
                {
                    name = "wooden chair";
                }

                var itemid = item?.ItemID ?? 0;
                entries.Add(new ItemListEntry($"{name} ({itemDef.Resources[0].Amount} wood)", itemid, 0, i));
                item?.Delete();
            }
        }
        return entries.Count > 0 ? entries.ToArray() : Array.Empty<ItemListEntry>();
    }

    public static ItemListEntry[] Tables(Mobile from)
    {
        var entries = new List<ItemListEntry>();
        var craftItems = DefCarpentry.CraftSystem.CraftItems;
        for (int i = 0; i < 4; ++i)
        {
            var itemDef = craftItems[i + 15];
            var chance = itemDef.GetSuccessChance(from, typeof(Board), DefCarpentry.CraftSystem, false, out _);
            var resAmount = from.Backpack.GetAmount(typeof(Log)) + from.Backpack.GetAmount(typeof(Board));
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("gT", "g T").ToLower();
                if (name == "yewwoodtable")
                {
                    name = "wooden table";
                }

                if (name == "largetable")
                {
                    name = "large wooden table";
                }

                var itemid = item?.ItemID ?? 0;
                entries.Add(new ItemListEntry($"{name} ({itemDef.Resources[0].Amount} wood)", itemid, 0, i));
                item?.Delete();
            }
        }
        return entries.Count > 0 ? entries.ToArray() : Array.Empty<ItemListEntry>();
    }

    public static ItemListEntry[] Misc(Mobile from)
    {
        var entries = new List<ItemListEntry>();
        var craftItems = DefCarpentry.CraftSystem.CraftItems;
        double chance;
        int resAmount;
        // Containers (9 items)
        for (int i = 0; i < 9; ++i)
        {
            var itemDef = craftItems[i + 19];
            chance = itemDef.GetSuccessChance(from, typeof(Board), DefCarpentry.CraftSystem, false, out _);
            resAmount = from.Backpack.GetAmount(typeof(Log)) + from.Backpack.GetAmount(typeof(Board));
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("nB", "n B").Replace("lC", "l C").Replace("eC", "e C").Replace("mC", "m C").Replace("nC", "n C").Replace("yA", "y A").Replace("yB", "y B").Replace("nS", "n S").ToLower();
                var itemid = item?.ItemID ?? 0;
                entries.Add(new ItemListEntry($"{name} ({itemDef.Resources[0].Amount} wood)", itemid, 0, i));
                item?.Delete();
            }
        }
        // Misc (5 items)
        for (int i = 0; i < 5; ++i)
        {
            var itemDef = craftItems[i + 28];
            chance = itemDef.GetSuccessChance(from, typeof(Board), DefCarpentry.CraftSystem, false, out _);
            resAmount = from.Backpack.GetAmount(typeof(Log)) + from.Backpack.GetAmount(typeof(Board));
            if (chance > 0 && resAmount >= itemDef.Resources[0].Amount)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("sC", "s C").Replace("rS", "r S").Replace("dS", "d S").Replace("nS", "n S").Replace("gP", "g P").ToLower();
                var itemid = item?.ItemID ?? 0;
                if (i == 4)
                {
                    entries.Add(new ItemListEntry($"{name} ({itemDef.Resources[0].Amount} logs, {itemDef.Resources[0].Amount} cloth)", itemid, 0, i));
                }
                else
                {
                    entries.Add(new ItemListEntry($"{name} ({itemDef.Resources[0].Amount} logs)", itemid, 0, i));
                }

                item?.Delete();
            }
        }
        return entries.Count > 0 ? entries.ToArray() : Array.Empty<ItemListEntry>();
    }

    public override void OnResponse(NetState state, int index)
    {
        if (m_IsFrom == "Main")
        {
            if (index == 0)
            {
                var entries = Chairs(m_Mobile);
                if (entries.Length == 0)
                {
                    m_Mobile.SendAsciiMessage("You do not have the resources to craft any chairs.");
                    return;
                }
                m_Mobile.SendMenu(new CarpentryMenu(m_Mobile, entries, "Chairs", m_Tool));
            }
            else if (index == 1)
            {
                var entries = Tables(m_Mobile);
                if (entries.Length == 0)
                {
                    m_Mobile.SendAsciiMessage("You do not have the resources to craft any tables.");
                    return;
                }
                m_Mobile.SendMenu(new CarpentryMenu(m_Mobile, entries, "Tables", m_Tool));
            }
            else if (index == 2)
            {
                var entries = Misc(m_Mobile);
                if (entries.Length == 0)
                {
                    m_Mobile.SendAsciiMessage("You do not have the resources to craft any miscellaneous items.");
                    return;
                }
                m_Mobile.SendMenu(new CarpentryMenu(m_Mobile, entries, "Misc", m_Tool));
            }
        }
        else if (m_IsFrom == "Chairs")
        {
            var craftItems = DefCarpentry.CraftSystem.CraftItems;
            var itemDef = craftItems[index + 6];
            int num = DefCarpentry.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendAsciiMessage("You don't have the required skills to attempt this item.");
                m_Mobile.SendMenu(new CarpentryMenu(m_Mobile, Chairs(m_Mobile), "Chairs", m_Tool));
                return;
            }
            var context = DefCarpentry.CraftSystem.GetContext(m_Mobile);
            var res = itemDef.UseSubRes2 ? DefCarpentry.CraftSystem.CraftSubRes2 : DefCarpentry.CraftSystem.CraftSubRes;
            int resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            DefCarpentry.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Tables")
        {
            var craftItems = DefCarpentry.CraftSystem.CraftItems;
            var itemDef = craftItems[index + 15];
            int num = DefCarpentry.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendAsciiMessage("You don't have the required skills to attempt this item.");
                m_Mobile.SendMenu(new CarpentryMenu(m_Mobile, Tables(m_Mobile), "Tables", m_Tool));
                return;
            }
            var context = DefCarpentry.CraftSystem.GetContext(m_Mobile);
            var res = itemDef.UseSubRes2 ? DefCarpentry.CraftSystem.CraftSubRes2 : DefCarpentry.CraftSystem.CraftSubRes;
            int resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            DefCarpentry.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Misc")
        {
            var craftItems = DefCarpentry.CraftSystem.CraftItems;
            int offset = index < 9 ? 19 : 28 - 9;
            var itemDef = craftItems[index + offset];
            int num = DefCarpentry.CraftSystem.CanCraft(m_Mobile, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                m_Mobile.SendAsciiMessage("You don't have the required skills to attempt this item.");
                m_Mobile.SendMenu(new CarpentryMenu(m_Mobile, Misc(m_Mobile), "Misc", m_Tool));
                return;
            }
            var context = DefCarpentry.CraftSystem.GetContext(m_Mobile);
            var res = itemDef.UseSubRes2 ? DefCarpentry.CraftSystem.CraftSubRes2 : DefCarpentry.CraftSystem.CraftSubRes;
            int resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            DefCarpentry.CraftSystem.CreateItem(m_Mobile, itemDef.ItemType, type, m_Tool, itemDef);
        }
    }
}
