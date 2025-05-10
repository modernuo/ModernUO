using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Network;
using Server.Menus.ItemLists;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

public class TinkeringMenu : ItemListMenu
{
    private readonly Mobile m_Mobile;
    private readonly string m_IsFrom;
    private readonly BaseTool m_Tool;
    private readonly ItemListEntry[] m_Entries;
    private readonly List<Type> _typeMap;
    private readonly Type _selectedResourceType;

    // Explicit type lists for T2A/classic tinkering menu categories
    private static readonly Type[] WoodTypes = {
        typeof(JointingPlane), typeof(MouldingPlane), typeof(SmoothingPlane), typeof(ClockFrame), typeof(Axle), typeof(RollingPin)
    };
    private static readonly Type[] MetalTypes = {
        typeof(DartTrapCraft), typeof(PoisonTrapCraft), typeof(ExplosionTrapCraft),
        typeof(Scissors), typeof(MortarPestle), typeof(Scorp), typeof(TinkerTools), typeof(Hatchet), typeof(DrawKnife), typeof(SewingKit), typeof(Saw), typeof(DovetailSaw), typeof(Froe), typeof(Hammer), typeof(Tongs), typeof(SmithHammer), typeof(SledgeHammer), typeof(Inshave), typeof(Pickaxe), typeof(Lockpick), typeof(Skillet), typeof(FlourSifter), typeof(FletcherTools), typeof(MapmakersPen), typeof(ScribesPen),
        typeof(Gears), typeof(ClockParts), typeof(BarrelTap), typeof(Springs), typeof(SextantParts), typeof(BarrelHoops), typeof(Hinge), typeof(BolaBall),
        typeof(ButcherKnife), typeof(SpoonLeft), typeof(SpoonRight), typeof(Plate), typeof(ForkLeft), typeof(ForkRight), typeof(Cleaver), typeof(KnifeLeft), typeof(KnifeRight), typeof(Goblet), typeof(PewterMug), typeof(SkinningKnife),
        typeof(KeyRing), typeof(Candelabra), typeof(Scales), typeof(Key), typeof(Globe), typeof(Spyglass), typeof(Lantern), typeof(HeatingStand),
        typeof(AxleGears), typeof(ClockParts), typeof(SextantParts), typeof(ClockRight), typeof(ClockLeft), typeof(Sextant), typeof(Bola), typeof(PotionKeg),
        typeof(Shovel)
    };

    public TinkeringMenu(Mobile m, ItemListEntry[] entries, string isFrom, BaseTool tool, List<Type> typeMap = null, Type selectedResourceType = null)
        : base("Choose an item.", entries)
    {
        m_Mobile = m;
        m_IsFrom = isFrom;
        m_Tool = tool;
        m_Entries = entries;
        _typeMap = typeMap;
        _selectedResourceType = selectedResourceType;
    }

    public static ItemListEntry[] MainMenu()
    {
        return new ItemListEntry[]
        {
            new ItemListEntry("Wooden Items", 0x1BDD, 0, 0),
            new ItemListEntry("Metal Items", 0x1BF2, 0, 1)
        };
    }

    private static (ItemListEntry[], List<Type>) BuildEntriesAndTypeMap(Mobile from, Type[] types, Type resourceType, string resourceName)
    {
        var entries = new List<ItemListEntry>();
        var typeMap = new List<Type>();
        foreach (var type in types)
        {
            var itemDef = DefTinkering.CraftSystem.CraftItems.SearchFor(type);
            if (itemDef == null) continue;
            var res = itemDef.Resources[0];
            double chance = itemDef.GetSuccessChance(from, resourceType, DefTinkering.CraftSystem, false, out var allRequiredSkills);
            if ((from.Backpack?.GetAmount(resourceType) ?? 0) >= res.Amount && chance > 0.0)
            {
                var name = itemDef.ItemType.Name.ToLower();
                var itemid = itemDef.ItemId;
                entries.Add(new ItemListEntry($"{name} ({res.Amount} {resourceName})", itemid, 0, 0));
                typeMap.Add(type);
            }
        }
        return (entries.ToArray(), typeMap);
    }

    // Helper to dynamically get the correct ItemID for menu entries
    private static int GetMenuItemID(Type type)
    {
        // Check for [CraftItemID] attribute (for TrapCraft)
        var attr = type.GetCustomAttributes(typeof(CraftItemIDAttribute), false);
        if (attr.Length > 0 && attr[0] is CraftItemIDAttribute craftAttr)
            return craftAttr.ItemID;

        // Otherwise, try to instantiate and get ItemID
        try
        {
            var instance = Activator.CreateInstance(type) as Item;
            if (instance != null)
                return instance.ItemID;
        }
        catch
        {
            // Ignore instantiation errors
        }

        // Fallback: 0
        return 0;
    }

    public static ItemListEntry[] Wood(Mobile from, out List<Type> typeMap)
    {
        // Use explicit mapping for classic T2A menu
        Type[] types = { typeof(JointingPlane), typeof(MouldingPlane), typeof(SmoothingPlane), typeof(ClockFrame), typeof(Axle), typeof(RollingPin) };
        int[] itemIDs = { 0x1030, 0x102C, 0x1032, 0x104D, 0x105B, 0x1043 };
        string[] nameFixes = { "gP", "kF" };
        typeMap = new List<Type>();
        var entries = new List<ItemListEntry>();
        bool allRequiredSkills = true;

        for (int i = 0; i < types.Length; ++i)
        {
            var craftItem = DefTinkering.CraftSystem.CraftItems.SearchFor(types[i]);
            if (craftItem == null) continue;
            double chance = craftItem.GetSuccessChance(from, typeof(Log), DefTinkering.CraftSystem, false, out allRequiredSkills);
            var craftResource = craftItem.Resources[0];
            if ((chance > 0) && (from.Backpack?.GetAmount(typeof(Log)) >= craftResource.Amount))
            {
                string name = types[i].Name.ToLower();
                foreach (var fix in nameFixes)
                    name = name.Replace(fix, fix[0] + " " + fix[1]);
                int itemID = itemIDs[i];
                entries.Add(new ItemListEntry(name, itemID, 0, i));
                typeMap.Add(types[i]);
            }
        }
        return entries.ToArray();
    }

    public static ItemListEntry[] Metal(Mobile from, out List<Type> typeMap)
    {
        // Updated order as provided by the user
        Type[] types = {
            typeof(DartTrapCraft),
            typeof(ExplosionTrapCraft),
            typeof(PoisonTrapCraft),
            typeof(SewingKit),
            typeof(Gears),
            typeof(Springs),
            typeof(Hinge),
            typeof(DrawKnife),
            typeof(Froe),
            typeof(Inshave),
            typeof(Scorp),
            typeof(ButcherKnife),
            typeof(Scissors),
            typeof(Tongs),
            typeof(DovetailSaw),
            typeof(Saw),
            typeof(Hammer),
            typeof(SmithHammer),
            typeof(SledgeHammer),
            typeof(Shovel) // Only left shovel
        };
        int[] itemIDs = {
            4397, // Dart Trap (menu icon)
            4344, // Explosion Trap (menu icon)
            4424, // Poison Trap (menu icon)
            0xF9D,  // SewingKit
            0x1053, // Gears
            0x105D, // Springs
            0x1055, // Hinge
            0x10E4, // Draw Knife
            0x10E5, // Froe
            0x10E6, // Inshave
            0x10E7, // Scorp
            0x13F6, // Butcher Knife
            0xF9F,  // Scissors
            0xFB5,  // Tongs
            0x1028, // Dovetail Saw
            0x1034, // Saw
            0x102A, // Hammer
            0x13E3, // Smith Hammer
            0xFB5,  // Sledge Hammer (classic uses same as tongs, but you may want 0xFB6)
            0xF39   // Shovel left only
        };
        typeMap = new List<Type>();
        var entries = new List<ItemListEntry>();
        bool allRequiredSkills = true;
        int count = Math.Min(types.Length, itemIDs.Length);
        for (int i = 0; i < count; ++i)
        {
            var craftItem = DefTinkering.CraftSystem.CraftItems.SearchFor(types[i]);
            if (craftItem == null) continue;
            double chance = craftItem.GetSuccessChance(from, typeof(IronIngot), DefTinkering.CraftSystem, false, out allRequiredSkills);
            var craftResource = craftItem.Resources[0];
            if ((chance > 0) && (from.Backpack?.GetAmount(typeof(IronIngot)) >= craftResource.Amount))
            {
                string name = types[i].Name.ToLower();
                int itemID = itemIDs[i];
                entries.Add(new ItemListEntry(name, itemID, 0, i));
                typeMap.Add(types[i]);
            }
        }
        return entries.ToArray();
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = m_Mobile;
        var craftIndex = m_Entries[index].CraftIndex;

        if (m_IsFrom == "Main")
        {
            if (craftIndex == 0)
            {
                var entries = Wood(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any wooden items.");
                    return;
                }
                from.SendMenu(new TinkeringMenu(from, entries, "Wood", m_Tool, typeMap, typeof(Log)));
            }
            else if (craftIndex == 1)
            {
                var entries = Metal(from, out var typeMap);
                if (entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill to craft any metal items.");
                    return;
                }
                from.SendMenu(new TinkeringMenu(from, entries, "Metal", m_Tool, typeMap, typeof(IronIngot)));
            }
        }
        else if (_typeMap != null && index >= 0 && index < _typeMap.Count)
        {
            if (from.Backpack == null)
            {
                from.SendAsciiMessage("You do not have a backpack!");
                return;
            }
            // Special handling for traps in Metal menu (first 3 entries)
            if (m_IsFrom == "Metal" && index <= 2)
            {
                var type = _typeMap[index];
                var itemDef = DefTinkering.CraftSystem.CraftItems.SearchFor(type);
                if (itemDef != null)
                {
                    // Check if player has enough resources (IronIngot)
                    var craftResource = itemDef.Resources[0];
                    int amount = from.Backpack.GetAmount(typeof(IronIngot));
                    if (amount < craftResource.Amount)
                    {
                        from.SendAsciiMessage("You lack the resources to set this trap.");
                        return;
                    }
                    // Only call the craft system, do NOT call ResourceSelection or set any targeting here!
                    itemDef.Craft(from, DefTinkering.CraftSystem, typeof(IronIngot), m_Tool);
                }
                return;
            }

            // For all other items, check for resources and call ResourceSelection if needed
            var itemType = _typeMap[index];
            var craftItemDef = DefTinkering.CraftSystem.CraftItems.SearchFor(itemType);
            if (craftItemDef != null)
            {
                Type resourceType = _selectedResourceType;
                if (resourceType == null)
                {
                    from.SendAsciiMessage("No resource type selected or available for this item.");
                    return;
                }
                int amount = from.Backpack.GetAmount(resourceType);
                if (amount < craftItemDef.Resources[0].Amount)
                {
                    ResourceSelection(from, m_Tool);
                    return;
                }
                craftItemDef.Craft(from, DefTinkering.CraftSystem, resourceType, m_Tool);
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
            from.SendAsciiMessage("Select the resource you wish to use (log or iron ingot).");
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Item item)
            {
                if (item is Log)
                {
                    var entries = TinkeringMenu.Wood(from, out var typeMap);
                    if (entries.Length == 0)
                    {
                        from.SendAsciiMessage("You lack the skill to craft anything with this resource.");
                        return;
                    }
                    from.SendMenu(new TinkeringMenu(from, entries, "Wood", m_Tool, typeMap, typeof(Log)));
                }
                else if (item is IronIngot)
                {
                    var entries = TinkeringMenu.Metal(from, out var typeMap);
                    if (entries.Length == 0)
                    {
                        from.SendAsciiMessage("You lack the skill to craft anything with this resource.");
                        return;
                    }
                    from.SendMenu(new TinkeringMenu(from, entries, "Metal", m_Tool, typeMap, typeof(IronIngot)));
                }
                else
                {
                    from.SendAsciiMessage("That is not a valid resource. Please select log or iron ingot.");
                    from.Target = new ResourceSelectTarget(m_From, m_Tool);
                }
            }
            else
            {
                from.SendAsciiMessage("That is not a valid resource. Please select log or iron ingot.");
                from.Target = new ResourceSelectTarget(m_From, m_Tool);
            }
        }
    }
} 