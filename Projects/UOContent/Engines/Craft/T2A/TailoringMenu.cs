using System;
using System.Collections.Generic;
using Server.Items;
using Server.Network;
using Server.Menus.ItemLists;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

public class TailoringMenu : ItemListMenu
{
    private readonly Mobile m_Mobile;
    private string m_IsFrom;
    private readonly BaseTool m_Tool;
    private readonly ItemListEntry[] m_Entries;
    private readonly List<Type> _typeMap;

    // Explicit type lists for T2A/classic tailoring menu categories
    private static readonly Type[] ShirtsTypes = {
        typeof(Doublet), typeof(Shirt), typeof(FancyShirt), typeof(Tunic), typeof(Surcoat), typeof(PlainDress)
    };
    private static readonly Type[] PantsTypes = {
        typeof(ShortPants), typeof(LongPants), typeof(Kilt)
    };
    private static readonly Type[] MiscTypes = {
        typeof(Skirt), typeof(Cloak), typeof(Robe), typeof(JesterSuit), typeof(FancyDress)
    };
    private static readonly Type[] BoltTypes = {
        typeof(BoltOfCloth)
    };
    private static readonly Type[] FootwearTypes = {
        typeof(Sandals), typeof(Shoes), typeof(Boots), typeof(ThighBoots)
    };
    private static readonly Type[] LeatherArmorTypes = {
        typeof(LeatherChest), typeof(LeatherGorget), typeof(LeatherGloves), typeof(LeatherCap), typeof(LeatherArms), typeof(LeatherLegs)
    };
    private static readonly Type[] StuddedArmorTypes = {
        typeof(StuddedChest), typeof(StuddedGorget), typeof(StuddedGloves), typeof(StuddedArms), typeof(StuddedLegs)
    };
    private static readonly Type[] FemaleArmorTypes = {
        typeof(FemaleLeatherChest), typeof(FemaleStuddedChest), typeof(LeatherBustierArms), typeof(StuddedBustierArms), typeof(FemalePlateChest), typeof(LeatherShorts), typeof(LeatherSkirt)
    };

    public TailoringMenu(Mobile m, ItemListEntry[] entries, string isFrom, BaseTool tool, List<Type> typeMap = null)
        : base("Choose an item.", entries)
    {
        m_Mobile = m;
        m_IsFrom = isFrom;
        m_Tool = tool;
        m_Entries = entries;
        _typeMap = typeMap;
    }

    public static ItemListEntry[] Main(Mobile from)
    {
        int resAmount = from.Backpack?.GetAmount(typeof(Cloth)) ?? 0;
        int missing = 0;
        var entries = new ItemListEntry[4];

        // Shirts
        var shirtsRes = DefTailoring.CraftSystem.CraftItems[12].Resources[0];
        if (resAmount >= shirtsRes.Amount)
        {
            entries[0 - missing] = new ItemListEntry("Shirts", 0x1517);
        }
        else
        {
            missing++;
        }

        // Pants
        var pantsRes = DefTailoring.CraftSystem.CraftItems[6].Resources[0];
        if (resAmount >= pantsRes.Amount)
        {
            entries[1 - missing] = new ItemListEntry("Pants", 0x1539, 0, 1);
        }
        else
        {
            missing++;
        }

        // Miscellaneous
        var miscRes = DefTailoring.CraftSystem.CraftItems[9].Resources[0];
        if (resAmount >= miscRes.Amount)
        {
            entries[2 - missing] = new ItemListEntry("Miscellaneous", 0x153D, 0, 2);
        }
        else
        {
            missing++;
        }

        // Bolt of Cloth
        var boltRes = DefTailoring.CraftSystem.CraftItems[14].Resources[0];
        if (resAmount >= boltRes.Amount)
        {
            entries[3 - missing] = new ItemListEntry("Bolt of Cloth", 0x0F95, 0, 3);
        }
        else
        {
            missing++;
        }

        Array.Resize(ref entries, entries.Length - missing);
        return entries;
    }

    // Helper method to build entries from type lists and return both entries and type map
    private static (ItemListEntry[], List<Type>) BuildEntriesAndTypeMap(Mobile from, Type[] types, string resourceName)
    {
        var entries = new List<ItemListEntry>();
        var typeMap = new List<Type>();
        foreach (var type in types)
        {
            var itemDef = DefTailoring.CraftSystem.CraftItems.SearchFor(type);
            if (itemDef == null)
            {
                continue;
            }

            var res = itemDef.Resources[0];
            double chance = itemDef.GetSuccessChance(from, typeof(Cloth), DefTailoring.CraftSystem, false, out var allRequiredSkills);
            if ((from.Backpack?.GetAmount(typeof(Cloth)) ?? 0) >= res.Amount && chance > 0.0)
            {
                var name = itemDef.ItemType.Name.ToLower();
                var itemid = itemDef.ItemId;
                entries.Add(new ItemListEntry($"{name} ({res.Amount} {resourceName})", itemid));
                typeMap.Add(type);
            }
        }
        return (entries.ToArray(), typeMap);
    }

    // Update menu methods to use explicit type lists
    public static ItemListEntry[] Shirts(Mobile from, out List<Type> typeMap)
    {
        var (entries, map) = BuildEntriesAndTypeMap(from, ShirtsTypes, "cloth");
        typeMap = map;
        return entries;
    }
    public static ItemListEntry[] Pants(Mobile from, out List<Type> typeMap)
    {
        var (entries, map) = BuildEntriesAndTypeMap(from, PantsTypes, "cloth");
        typeMap = map;
        return entries;
    }
    public static ItemListEntry[] Misc(Mobile from, out List<Type> typeMap)
    {
        var (entries, map) = BuildEntriesAndTypeMap(from, MiscTypes, "cloth");
        typeMap = map;
        return entries;
    }
    public static ItemListEntry[] Bolt(Mobile from, out List<Type> typeMap)
    {
        var entries = new List<ItemListEntry>();
        typeMap = new List<Type>();
        // BoltOfCloth always requires 50 cloth per bolt
        int clothAmount = from.Backpack?.GetAmount(typeof(Cloth)) ?? 0;
        int bolts = clothAmount / 50;
        if (bolts > 0)
        {
            entries.Add(new ItemListEntry($"bolt of cloth (50 cloth)", 0xF95));
            typeMap.Add(typeof(BoltOfCloth));
        }
        return entries.ToArray();
    }
    public static ItemListEntry[] Footwear(Mobile from, out List<Type> typeMap)
    {
        var (entries, map) = BuildEntriesAndTypeMap(from, FootwearTypes, "leather");
        typeMap = map;
        return entries;
    }
    public static ItemListEntry[] Leather(Mobile from, out List<Type> typeMap)
    {
        var (entries, map) = BuildEntriesAndTypeMap(from, LeatherArmorTypes, "leather");
        typeMap = map;
        return entries;
    }
    public static ItemListEntry[] Studded(Mobile from, out List<Type> typeMap)
    {
        var (entries, map) = BuildEntriesAndTypeMap(from, StuddedArmorTypes, "leather");
        typeMap = map;
        return entries;
    }
    public static ItemListEntry[] Female(Mobile from, out List<Type> typeMap)
    {
        var (entries, map) = BuildEntriesAndTypeMap(from, FemaleArmorTypes, "leather");
        typeMap = map;
        return entries;
    }

    // Add a new method to build the LeatherMain menu
    public static ItemListEntry[] LeatherMain(Mobile from)
    {
        return new ItemListEntry[]
        {
            new ItemListEntry("Footwear", 0x170f),
            new ItemListEntry("Leather Armor", 0x13cc, 0, 1),
            new ItemListEntry("Studded Armor", 0x13db, 0, 2),
            new ItemListEntry("Female Armor", 0x1c06, 0, 3)
        };
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = m_Mobile;
        var craftIndex = m_Entries[index].CraftIndex;

        if (m_IsFrom == "Main")
        {
            if (craftIndex == 0)
            {
                m_IsFrom = "Shirts";
                var entries = Shirts(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any shirts.");
                    return;
                }
                from.SendMenu(new TailoringMenu(from, entries, m_IsFrom, m_Tool, typeMap));
            }
            else if (craftIndex == 1)
            {
                m_IsFrom = "Pants";
                var entries = Pants(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any pants.");
                    return;
                }
                from.SendMenu(new TailoringMenu(from, entries, m_IsFrom, m_Tool, typeMap));
            }
            else if (craftIndex == 2)
            {
                m_IsFrom = "Misc";
                var entries = Misc(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any miscellaneous items.");
                    return;
                }
                from.SendMenu(new TailoringMenu(from, entries, m_IsFrom, m_Tool, typeMap));
            }
            else if (craftIndex == 3)
            {
                m_IsFrom = "BoltOfCloth";
                var entries = Bolt(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any bolts of cloth.");
                    return;
                }
                from.SendMenu(new TailoringMenu(from, entries, m_IsFrom, m_Tool, typeMap));
            }
        }
        else if (m_IsFrom == "LeatherMain")
        {
            if (craftIndex == 0)
            {
                m_IsFrom = "Footwear";
                var entries = Footwear(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any footwear.");
                    return;
                }
                from.SendMenu(new TailoringMenu(from, entries, m_IsFrom, m_Tool, typeMap));
            }
            else if (craftIndex == 1)
            {
                m_IsFrom = "Leather";
                var entries = Leather(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any leather armor.");
                    return;
                }
                from.SendMenu(new TailoringMenu(from, entries, m_IsFrom, m_Tool, typeMap));
            }
            else if (craftIndex == 2)
            {
                m_IsFrom = "Studded";
                var entries = Studded(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any studded armor.");
                    return;
                }
                from.SendMenu(new TailoringMenu(from, entries, m_IsFrom, m_Tool, typeMap));
            }
            else if (craftIndex == 3)
            {
                m_IsFrom = "Female";
                var entries = Female(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any female armor.");
                    return;
                }
                from.SendMenu(new TailoringMenu(from, entries, m_IsFrom, m_Tool, typeMap));
            }
        }
        else if (_typeMap != null && index >= 0 && index < _typeMap.Count)
        {
            // Use the mapped type to craft the correct item
            var type = _typeMap[index];
            var itemDef = DefTailoring.CraftSystem.CraftItems.SearchFor(type);
            if (itemDef != null)
            {
                var num = DefTailoring.CraftSystem.CanCraft(from, m_Tool, itemDef.ItemType);
                if (num > 0)
                {
                    from.SendLocalizedMessage(num); // Failure message
                    return;
                }
                var context = DefTailoring.CraftSystem.GetContext(from);
                var res = itemDef.UseSubRes2 ? DefTailoring.CraftSystem.CraftSubRes2 : DefTailoring.CraftSystem.CraftSubRes;
                var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
                Type resourceType = resIndex > -1 ? res[resIndex].ItemType : null;
                DefTailoring.CraftSystem.CreateItem(from, itemDef.ItemType, resourceType, m_Tool, itemDef);
                from.SendAsciiMessage($"You have crafted a {itemDef.ItemType.Name.ToLower().Replace("_", " ") }.");
            }
        }
    }

    public static void ResourceSelection(Mobile from, BaseTool tool)
    {
        from.Target = new ResourceSelectTarget(from, tool);
    }

    private class ResourceSelectTarget : Target
    {
        private readonly Mobile m_From;
        private readonly BaseTool m_Tool;

        public ResourceSelectTarget(Mobile from, BaseTool tool)
            : base(12, false, TargetFlags.None)
        {
            m_From = from;
            m_Tool = tool;
            from.SendAsciiMessage("Select the resource you wish to use (cloth, leather, or hides).");
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Item item)
            {
                if (item is Cloth)
                {
                    var entries = TailoringMenu.Main(from);
                    if (entries.Length == 0)
                    {
                        from.SendAsciiMessage("You lack the skill to craft anything with this resource.");
                        return;
                    }
                    from.SendMenu(new TailoringMenu(from, entries, "Main", m_Tool));
                }
                else if (item is Leather || item is Hides)
                {
                    var entries = TailoringMenu.LeatherMain(from);
                    if (entries.Length == 0)
                    {
                        from.SendAsciiMessage("You lack the skill to craft anything with this resource.");
                        return;
                    }
                    from.SendMenu(new TailoringMenu(from, entries, "LeatherMain", m_Tool));
                }
                else
                {
                    from.SendAsciiMessage("That is not a valid resource. Please select cloth, leather, or hides.");
                    from.Target = new ResourceSelectTarget(m_From, m_Tool);
                }
            }
            else
            {
                from.SendAsciiMessage("That is not a valid resource. Please select cloth, leather, or hides.");
                from.Target = new ResourceSelectTarget(m_From, m_Tool);
            }
        }
    }

    // TODO: Verify the indices and counts for each submenu (Shirts, Pants, Misc, Bolt, Footwear, Leather, Studded, Female) match the actual order in DefTailoring.CraftSystem.CraftItems.
    // If the indices are wrong, the wrong items will appear in the submenus.
} 