using System;
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Network;
using Server.Menus.ItemLists;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

public class BlacksmithMenu : ItemListMenu
{
    private readonly Mobile m_Mobile;
    private string m_IsFrom;
    private readonly BaseTool m_Tool;
    private readonly ItemListEntry[] m_Entries;

    public BlacksmithMenu(Mobile m, ItemListEntry[] entries, string isFrom, BaseTool tool)
        : base("Choose an item.", entries)
    {
        m_Mobile = m;
        m_IsFrom = isFrom;
        m_Tool = tool;
        m_Entries = entries;
    }

    public static ItemListEntry[] Main(Mobile from)
    {
        bool shields = true, armor = true, weapons = true;
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;

        // Shields
        chance = DefBlacksmithy.CraftSystem.CraftItems[23].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[23].Resources[0].Amount || chance <= 0.0)
            shields = false;

        // Armor
        chance = DefBlacksmithy.CraftSystem.CraftItems[0].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[0].Resources[0].Amount || chance <= 0.0)
            armor = false;

        // Weapons
        chance = DefBlacksmithy.CraftSystem.CraftItems[26].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[26].Resources[0].Amount || chance <= 0.0)
            weapons = false;

        var entries = new ItemListEntry[4];
        entries[0] = new ItemListEntry("Repair", 4015, 0, 0);
        if (shields) entries[1 - missing] = new ItemListEntry("Shields", 7026, 0, 1); else missing++;
        if (armor) entries[2 - missing] = new ItemListEntry("Armor", 5141, 0, 2); else missing++;
        if (weapons) entries[3 - missing] = new ItemListEntry("Weapons", 5049, 0, 3); else missing++;
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Shields(Mobile from)
    {
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        var entries = new ItemListEntry[6];
        for (int i = 0; i < 6; ++i)
        {
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[i + 18];
            var res = itemDef.Resources[0];
            chance = itemDef.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("S", " S").Replace("K", " K").ToLower();
                var itemid = item?.ItemID ?? 0;
                if (itemid == 7033) itemid = 7032;
                entries[i - missing] = new ItemListEntry($"{name} ({res.Amount} ingots)", itemid, 0, i);
                item?.Delete();
            }
            else { missing++; }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Weapons(Mobile from)
    {
        bool swords = true, axes = true, maces = true, polearms = true;
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        // Swords
        chance = DefBlacksmithy.CraftSystem.CraftItems[26].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[26].Resources[0].Amount || chance <= 0.0) swords = false;
        // Axes
        chance = DefBlacksmithy.CraftSystem.CraftItems[34].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[34].Resources[0].Amount || chance <= 0.0) axes = false;
        // Maces
        chance = DefBlacksmithy.CraftSystem.CraftItems[45].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[45].Resources[0].Amount || chance <= 0.0) maces = false;
        // Polearms
        chance = DefBlacksmithy.CraftSystem.CraftItems[42].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[42].Resources[0].Amount || chance <= 0.0) polearms = false;
        var entries = new ItemListEntry[4];
        if (swords) entries[0 - missing] = new ItemListEntry("Swords & Blades", 5049, 0, 0); else missing++;
        if (axes) entries[1 - missing] = new ItemListEntry("Axes", 3913, 0, 1); else missing++;
        if (maces) entries[2 - missing] = new ItemListEntry("Maces & Hammers", 5127, 0, 2); else missing++;
        if (polearms) entries[3 - missing] = new ItemListEntry("Polearms", 3917, 0, 3); else missing++;
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Blades(Mobile from)
    {
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        var entries = new ItemListEntry[8];
        for (int i = 0; i < 8; ++i)
        {
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[i + 24];
            var res = itemDef.Resources[0];
            chance = itemDef.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.ToLower();
                var itemid = item?.ItemID ?? 0;
                entries[i - missing] = new ItemListEntry($"{name} ({res.Amount} ingots)", itemid, 0, i);
                item?.Delete();
            }
            else { missing++; }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Axes(Mobile from)
    {
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        var entries = new ItemListEntry[7];
        for (int i = 0; i < 7; ++i)
        {
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[i + 32];
            var res = itemDef.Resources[0];
            chance = itemDef.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("B", " B").Replace("H", " H").Replace("A", " A").Trim().ToLower();
                var itemid = item?.ItemID ?? 0;
                entries[i - missing] = new ItemListEntry($"{name} ({res.Amount} ingots)", itemid, 0, i);
                item?.Delete();
            }
            else { missing++; }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Maces(Mobile from)
    {
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        var entries = new ItemListEntry[6];
        for (int i = 0; i < 6; ++i)
        {
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[i + 43];
            var res = itemDef.Resources[0];
            chance = itemDef.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("P", " P").Replace("F", " F").Replace("M", " M").Replace("H", " H").Trim().ToLower();
                var itemid = item?.ItemID ?? 0;
                entries[i - missing] = new ItemListEntry($"{name} ({res.Amount} ingots)", itemid, 0, i);
                item?.Delete();
            }
            else { missing++; }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Polearms(Mobile from)
    {
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        var entries = new ItemListEntry[4];
        for (int i = 0; i < 4; ++i)
        {
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[i + 39];
            var res = itemDef.Resources[0];
            chance = itemDef.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("S", " S").Trim().ToLower();
                var itemid = item?.ItemID ?? 0;
                entries[i - missing] = new ItemListEntry($"{name} ({res.Amount} ingots)", itemid, 0, i);
                item?.Delete();
            }
            else { missing++; }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Armor(Mobile from)
    {
        bool platemail = true, chainmail = true, ringmail = true, helmets = true;
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        // Platemail
        chance = DefBlacksmithy.CraftSystem.CraftItems[9].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[9].Resources[0].Amount || chance <= 0.0) platemail = false;
        // Chainmail
        chance = DefBlacksmithy.CraftSystem.CraftItems[5].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[5].Resources[0].Amount || chance <= 0.0) chainmail = false;
        // Ringmail
        chance = DefBlacksmithy.CraftSystem.CraftItems[0].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[0].Resources[0].Amount || chance <= 0.0) ringmail = false;
        // Helmets
        chance = DefBlacksmithy.CraftSystem.CraftItems[4].GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) < DefBlacksmithy.CraftSystem.CraftItems[4].Resources[0].Amount || chance <= 0.0) helmets = false;
        var entries = new ItemListEntry[4];
        if (platemail) entries[0 - missing] = new ItemListEntry("Platemail", 5141, 0, 0); else missing++;
        if (chainmail) entries[1 - missing] = new ItemListEntry("Chainmail", 5055, 0, 1); else missing++;
        if (ringmail) entries[2 - missing] = new ItemListEntry("Ringmail", 5100, 0, 2); else missing++;
        if (helmets) entries[3 - missing] = new ItemListEntry("Helmets", 5138, 0, 3); else missing++;
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Platemail(Mobile from)
    {
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        var entries = new ItemListEntry[6];
        for (int i = 0; i < 6; ++i)
        {
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[i + 7];
            var res = itemDef.Resources[0];
            chance = itemDef.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("A", " A").Replace("G", " G").Replace("L", " L").Replace("C", " C").Replace("Female", "").Replace("Plate", "Platemail").Trim().ToLower();
                var itemid = item?.ItemID ?? 0;
                entries[i - missing] = new ItemListEntry($"{name} ({res.Amount} ingots)", itemid, 0, i);
                item?.Delete();
            }
            else { missing++; }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Chainmail(Mobile from)
    {
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        var entries = new ItemListEntry[2];
        for (int i = 0; i < 2; ++i)
        {
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[i + 5];
            var res = itemDef.Resources[0];
            chance = itemDef.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("Chest", " Tunic").Replace("L", " L").Replace("Chain", " Chainmail").Trim().ToLower();
                var itemid = item?.ItemID ?? 0;
                entries[i - missing] = new ItemListEntry($"{name} ({res.Amount} ingots)", itemid, 0, i);
                item?.Delete();
            }
            else { missing++; }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Ringmail(Mobile from)
    {
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        var entries = new ItemListEntry[4];
        for (int i = 0; i < 4; ++i)
        {
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[i];
            var res = itemDef.Resources[0];
            chance = itemDef.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("Chest", "Tunic").Replace("T", " T").Replace("L", " L").Replace("S", " S").Replace("G", " G").Replace("A", " A").Trim().ToLower();
                var itemid = item?.ItemID ?? 0;
                entries[i - missing] = new ItemListEntry($"{name} ({res.Amount} ingots)", itemid, 0, i);
                item?.Delete();
            }
            else { missing++; }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public static ItemListEntry[] Helmets(Mobile from)
    {
        int missing = 0;
        bool allRequiredSkills = true;
        double chance;
        var entries = new ItemListEntry[6];
        // chainmail coif
        var itemDef0 = DefBlacksmithy.CraftSystem.CraftItems[4];
        var res0 = itemDef0.Resources[0];
        chance = itemDef0.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
        if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res0.Amount && chance > 0.0)
        {
            var item = Activator.CreateInstance(itemDef0.ItemType) as Item;
            var name = item?.GetType().Name ?? "";
            name = name.Replace("Chain", "Chainmail").Replace("C", " C").Trim().ToLower();
            var itemid = item?.ItemID ?? 0;
            entries[0] = new ItemListEntry($"{name} ({res0.Amount} ingots)", itemid, 0, 0);
            item?.Delete();
        }
        else { missing++; }
        // the rest
        for (int i = 1; i < 6; ++i)
        {
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[i + 12];
            var res = itemDef.Resources[0];
            chance = itemDef.GetSuccessChance(from, typeof(IronIngot), DefBlacksmithy.CraftSystem, false, out allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(IronIngot)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var item = Activator.CreateInstance(itemDef.ItemType) as Item;
                var name = item?.GetType().Name ?? "";
                name = name.Replace("C", " C").Replace("H", " H").Trim().ToLower();
                var itemid = item?.ItemID ?? 0;
                entries[i - missing] = new ItemListEntry($"{name} ({res.Amount} ingots)", itemid, 0, i);
                item?.Delete();
            }
            else { missing++; }
        }
        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = m_Mobile;
        var craftIndex = m_Entries[index].CraftIndex;
        Console.WriteLine($"[BlacksmithMenu.OnResponse] State={m_IsFrom}, Index={index}, CraftIndex={craftIndex}, Player={from?.Name}");
        if (m_IsFrom == "Main")
        {
            if (craftIndex == 0)
            {
                Console.WriteLine("[BlacksmithMenu] Repair selected");
                Repair.Do(from, DefBlacksmithy.CraftSystem, m_Tool);
            }
            else if (craftIndex == 1)
            {
                Console.WriteLine("[BlacksmithMenu] Shields submenu selected");
                var context = DefBlacksmithy.CraftSystem.GetContext(from);
                if (context == null || context.LastResourceIndex < 0)
                {
                    ResourceSelection(from, m_Tool, (mob, tool) => mob.SendMenu(new BlacksmithMenu(mob, Shields(mob), "Shields", tool)));
                }
                else
                {
                    from.SendMenu(new BlacksmithMenu(from, Shields(from), "Shields", m_Tool));
                }
            }
            else if (craftIndex == 2)
            {
                Console.WriteLine("[BlacksmithMenu] Armor submenu selected");
                var context = DefBlacksmithy.CraftSystem.GetContext(from);
                if (context == null || context.LastResourceIndex < 0)
                {
                    ResourceSelection(from, m_Tool, (mob, tool) => mob.SendMenu(new BlacksmithMenu(mob, Armor(mob), "Armor", tool)));
                }
                else
                {
                    from.SendMenu(new BlacksmithMenu(from, Armor(from), "Armor", m_Tool));
                }
            }
            else if (craftIndex == 3)
            {
                Console.WriteLine("[BlacksmithMenu] Weapons submenu selected");
                var context = DefBlacksmithy.CraftSystem.GetContext(from);
                if (context == null || context.LastResourceIndex < 0)
                {
                    ResourceSelection(from, m_Tool, (mob, tool) => mob.SendMenu(new BlacksmithMenu(mob, Weapons(mob), "Weapons", tool)));
                }
                else
                {
                    from.SendMenu(new BlacksmithMenu(from, Weapons(from), "Weapons", m_Tool));
                }
            }
            else
            {
                Console.WriteLine($"[BlacksmithMenu] Unknown craftIndex in Main: {craftIndex}");
            }
        }
        else if (m_IsFrom == "Shields")
        {
            var idx = craftIndex + 18;
            Console.WriteLine($"[BlacksmithMenu] Shields item selected, idx={idx}");
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[idx];
            var num = DefBlacksmithy.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                if (num == 1044037)
                    from.SendMessage("You do not have enough ingots to make that.");
                else
                    from.SendLocalizedMessage(num);
                from.SendMenu(new BlacksmithMenu(from, Shields(from), m_IsFrom, m_Tool));
                return;
            }
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            var res = itemDef.UseSubRes2 ? DefBlacksmithy.CraftSystem.CraftSubRes2 : DefBlacksmithy.CraftSystem.CraftSubRes;
            var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            Console.WriteLine($"[BlacksmithMenu] Crafting item: {itemDef.ItemType}, resource type: {type}");
            DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Weapons")
        {
            if (craftIndex == 0)
            {
                Console.WriteLine("[BlacksmithMenu] Blades submenu selected");
                m_IsFrom = "Blades";
                from.SendMenu(new BlacksmithMenu(from, Blades(from), m_IsFrom, m_Tool));
            }
            else if (craftIndex == 1)
            {
                Console.WriteLine("[BlacksmithMenu] Axes submenu selected");
                m_IsFrom = "Axes";
                from.SendMenu(new BlacksmithMenu(from, Axes(from), m_IsFrom, m_Tool));
            }
            else if (craftIndex == 2)
            {
                Console.WriteLine("[BlacksmithMenu] Maces submenu selected");
                m_IsFrom = "Maces";
                from.SendMenu(new BlacksmithMenu(from, Maces(from), m_IsFrom, m_Tool));
            }
            else if (craftIndex == 3)
            {
                Console.WriteLine("[BlacksmithMenu] Polearms submenu selected");
                m_IsFrom = "Polearms";
                from.SendMenu(new BlacksmithMenu(from, Polearms(from), m_IsFrom, m_Tool));
            }
            else
            {
                Console.WriteLine($"[BlacksmithMenu] Unknown craftIndex in Weapons: {craftIndex}");
            }
        }
        else if (m_IsFrom == "Blades")
        {
            var idx = craftIndex + 24;
            Console.WriteLine($"[BlacksmithMenu] Blades item selected, idx={idx}");
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[idx];
            var num = DefBlacksmithy.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                if (num == 1044037)
                    from.SendMessage("You do not have enough ingots to make that.");
                else
                    from.SendLocalizedMessage(num);
                from.SendMenu(new BlacksmithMenu(from, Blades(from), m_IsFrom, m_Tool));
                return;
            }
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            var res = itemDef.UseSubRes2 ? DefBlacksmithy.CraftSystem.CraftSubRes2 : DefBlacksmithy.CraftSystem.CraftSubRes;
            var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            Console.WriteLine($"[BlacksmithMenu] Crafting item: {itemDef.ItemType}, resource type: {type}");
            DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Axes")
        {
            var idx = craftIndex + 32;
            Console.WriteLine($"[BlacksmithMenu] Axes item selected, idx={idx}");
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[idx];
            var num = DefBlacksmithy.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                if (num == 1044037)
                    from.SendMessage("You do not have enough ingots to make that.");
                else
                    from.SendLocalizedMessage(num);
                from.SendMenu(new BlacksmithMenu(from, Axes(from), m_IsFrom, m_Tool));
                return;
            }
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            var res = itemDef.UseSubRes2 ? DefBlacksmithy.CraftSystem.CraftSubRes2 : DefBlacksmithy.CraftSystem.CraftSubRes;
            var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            Console.WriteLine($"[BlacksmithMenu] Crafting item: {itemDef.ItemType}, resource type: {type}");
            DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Maces")
        {
            var idx = craftIndex + 43;
            Console.WriteLine($"[BlacksmithMenu] Maces item selected, idx={idx}");
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[idx];
            var num = DefBlacksmithy.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                if (num == 1044037)
                    from.SendMessage("You do not have enough ingots to make that.");
                else
                    from.SendLocalizedMessage(num);
                from.SendMenu(new BlacksmithMenu(from, Maces(from), m_IsFrom, m_Tool));
                return;
            }
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            var res = itemDef.UseSubRes2 ? DefBlacksmithy.CraftSystem.CraftSubRes2 : DefBlacksmithy.CraftSystem.CraftSubRes;
            var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            Console.WriteLine($"[BlacksmithMenu] Crafting item: {itemDef.ItemType}, resource type: {type}");
            DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Polearms")
        {
            var idx = craftIndex + 39;
            Console.WriteLine($"[BlacksmithMenu] Polearms item selected, idx={idx}");
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[idx];
            var num = DefBlacksmithy.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                if (num == 1044037)
                    from.SendMessage("You do not have enough ingots to make that.");
                else
                    from.SendLocalizedMessage(num);
                from.SendMenu(new BlacksmithMenu(from, Polearms(from), m_IsFrom, m_Tool));
                return;
            }
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            var res = itemDef.UseSubRes2 ? DefBlacksmithy.CraftSystem.CraftSubRes2 : DefBlacksmithy.CraftSystem.CraftSubRes;
            var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            Console.WriteLine($"[BlacksmithMenu] Crafting item: {itemDef.ItemType}, resource type: {type}");
            DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Armor")
        {
            if (craftIndex == 0)
            {
                Console.WriteLine("[BlacksmithMenu] Platemail submenu selected");
                m_IsFrom = "Platemail";
                from.SendMenu(new BlacksmithMenu(from, Platemail(from), m_IsFrom, m_Tool));
            }
            else if (craftIndex == 1)
            {
                Console.WriteLine("[BlacksmithMenu] Chainmail submenu selected");
                m_IsFrom = "Chainmail";
                from.SendMenu(new BlacksmithMenu(from, Chainmail(from), m_IsFrom, m_Tool));
            }
            else if (craftIndex == 2)
            {
                Console.WriteLine("[BlacksmithMenu] Ringmail submenu selected");
                m_IsFrom = "Ringmail";
                from.SendMenu(new BlacksmithMenu(from, Ringmail(from), m_IsFrom, m_Tool));
            }
            else if (craftIndex == 3)
            {
                Console.WriteLine("[BlacksmithMenu] Helmets submenu selected");
                m_IsFrom = "Helmets";
                from.SendMenu(new BlacksmithMenu(from, Helmets(from), m_IsFrom, m_Tool));
            }
            else
            {
                Console.WriteLine($"[BlacksmithMenu] Unknown craftIndex in Armor: {craftIndex}");
            }
        }
        else if (m_IsFrom == "Platemail")
        {
            var idx = craftIndex + 7;
            Console.WriteLine($"[BlacksmithMenu] Platemail item selected, idx={idx}");
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[idx];
            var num = DefBlacksmithy.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                if (num == 1044037)
                    from.SendMessage("You do not have enough ingots to make that.");
                else
                    from.SendLocalizedMessage(num);
                from.SendMenu(new BlacksmithMenu(from, Platemail(from), m_IsFrom, m_Tool));
                return;
            }
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            var res = itemDef.UseSubRes2 ? DefBlacksmithy.CraftSystem.CraftSubRes2 : DefBlacksmithy.CraftSystem.CraftSubRes;
            var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            Console.WriteLine($"[BlacksmithMenu] Crafting item: {itemDef.ItemType}, resource type: {type}");
            DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Chainmail")
        {
            var idx = craftIndex + 5;
            Console.WriteLine($"[BlacksmithMenu] Chainmail item selected, idx={idx}");
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[idx];
            var num = DefBlacksmithy.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                if (num == 1044037)
                    from.SendMessage("You do not have enough ingots to make that.");
                else
                    from.SendLocalizedMessage(num);
                from.SendMenu(new BlacksmithMenu(from, Chainmail(from), m_IsFrom, m_Tool));
                return;
            }
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            var res = itemDef.UseSubRes2 ? DefBlacksmithy.CraftSystem.CraftSubRes2 : DefBlacksmithy.CraftSystem.CraftSubRes;
            var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            Console.WriteLine($"[BlacksmithMenu] Crafting item: {itemDef.ItemType}, resource type: {type}");
            DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Ringmail")
        {
            var idx = craftIndex;
            Console.WriteLine($"[BlacksmithMenu] Ringmail item selected, idx={idx}");
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[idx];
            var num = DefBlacksmithy.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                if (num == 1044037)
                    from.SendMessage("You do not have enough ingots to make that.");
                else
                    from.SendLocalizedMessage(num);
                from.SendMenu(new BlacksmithMenu(from, Ringmail(from), m_IsFrom, m_Tool));
                return;
            }
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            var res = itemDef.UseSubRes2 ? DefBlacksmithy.CraftSystem.CraftSubRes2 : DefBlacksmithy.CraftSystem.CraftSubRes;
            var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            Console.WriteLine($"[BlacksmithMenu] Crafting item: {itemDef.ItemType}, resource type: {type}");
            DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, m_Tool, itemDef);
        }
        else if (m_IsFrom == "Helmets")
        {
            var idx = craftIndex + 12;
            if (craftIndex == 0)
            {
                idx = 4;
            }
            Console.WriteLine($"[BlacksmithMenu] Helmets item selected, idx={idx}");
            var itemDef = DefBlacksmithy.CraftSystem.CraftItems[idx];
            var num = DefBlacksmithy.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
            if (num > 0)
            {
                if (num == 1044037)
                    from.SendMessage("You do not have enough ingots to make that.");
                else
                    from.SendLocalizedMessage(num);
                from.SendMenu(new BlacksmithMenu(from, Helmets(from), m_IsFrom, m_Tool));
                return;
            }
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            var res = itemDef.UseSubRes2 ? DefBlacksmithy.CraftSystem.CraftSubRes2 : DefBlacksmithy.CraftSystem.CraftSubRes;
            var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            Type type = resIndex > -1 ? res[resIndex].ItemType : null;
            Console.WriteLine($"[BlacksmithMenu] Crafting item: {itemDef.ItemType}, resource type: {type}");
            DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, m_Tool, itemDef);
        }
    }

    public override void OnCancel(NetState state)
    {
        base.OnCancel(state);
        var from = m_Mobile;
        var context = DefBlacksmithy.CraftSystem.GetContext(from);
        if (context != null)
        {
            context.LastResourceIndex = -1;
        }
    }

    public static void ResourceSelection(Mobile from, BaseTool tool, Action<Mobile, BaseTool> afterSelect)
    {
        var res = DefBlacksmithy.CraftSystem.CraftSubRes;
        int availableCount = 0;
        int lastAvailable = -1;
        for (int i = 0; i < res.Count; ++i)
        {
            var amount = from.Backpack?.GetAmount(res[i].ItemType) ?? 0;
            if (amount > 0)
            {
                availableCount++;
                lastAvailable = i;
            }
        }
        if (availableCount <= 1)
        {
            var context = DefBlacksmithy.CraftSystem.GetContext(from);
            if (context != null && lastAvailable != -1)
                context.LastResourceIndex = lastAvailable;
            afterSelect(from, tool);
        }
        else
        {
            from.SendMessage("Target the ingots you wish to use.");
            from.Target = new BlacksmithResourceTarget(tool, afterSelect);
        }
    }
}

// Target class for selecting ingot resource
public class BlacksmithResourceTarget : Target
{
    private readonly BaseTool m_Tool;
    private readonly Action<Mobile, BaseTool> m_AfterSelect;
    public BlacksmithResourceTarget(BaseTool tool, Action<Mobile, BaseTool> afterSelect) : base(2, false, TargetFlags.None)
    {
        m_Tool = tool;
        m_AfterSelect = afterSelect;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is Item item)
        {
            var res = DefBlacksmithy.CraftSystem.CraftSubRes;
            for (int i = 0; i < res.Count; ++i)
            {
                if (item.GetType() == res[i].ItemType)
                {
                    var context = DefBlacksmithy.CraftSystem.GetContext(from);
                    if (context != null)
                        context.LastResourceIndex = i;
                    m_AfterSelect(from, m_Tool);
                    return;
                }
            }
            from.SendMessage("That is not a valid ingot.");
            from.Target = new BlacksmithResourceTarget(m_Tool, m_AfterSelect);
        }
        else
        {
            from.SendMessage("That is not a valid ingot.");
            from.Target = new BlacksmithResourceTarget(m_Tool, m_AfterSelect);
        }
    }
} 