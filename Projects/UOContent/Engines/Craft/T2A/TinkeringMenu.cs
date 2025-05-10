using System;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

public class TinkeringMenu : ItemListMenu
{
    private enum Category
    {
        Main,
        Wood,
        Tools,
        Parts,
        Utensils,
        Traps,
        Misc,
        Jewelry,
        Necklaces,
        Earrings,
        Rings,
        Keg
    }

    private static readonly Type[] WoodItemTypes =
    [
        typeof(JointingPlane), typeof(MouldingPlane), typeof(SmoothingPlane),
        typeof(ClockFrame), typeof(Axle), typeof(RollingPin)
    ];

    private static readonly Type[] ToolTypes =
    [
        typeof(SewingKit), typeof(TinkerTools),
        typeof(DrawKnife), typeof(Froe), typeof(Inshave), typeof(Scorp),
        typeof(Scissors), typeof(Tongs),
        typeof(DovetailSaw), typeof(Saw), typeof(Hammer),
        typeof(SmithHammer), typeof(SledgeHammer), typeof(Shovel),
        typeof(MortarPestle), typeof(Hatchet), typeof(Pickaxe), typeof(Lockpick)
    ];

    private static readonly Type[] PartTypes =
    [
        typeof(Gears), typeof(Springs), typeof(Hinge),
        typeof(ClockParts), typeof(SextantParts),
        typeof(BarrelTap), typeof(BarrelHoops)
    ];

    private static readonly Type[] UtensilTypes =
    [
        typeof(ButcherKnife), typeof(Plate), typeof(Cleaver),
        typeof(KnifeLeft), typeof(KnifeRight), typeof(SkinningKnife),
        typeof(ForkLeft), typeof(ForkRight),
        typeof(SpoonLeft), typeof(SpoonRight),
        typeof(Goblet), typeof(PewterMug)
    ];

    private static readonly Type[] TrapTypes =
    [
        typeof(DartTrapCraft), typeof(ExplosionTrapCraft), typeof(PoisonTrapCraft)
    ];

    private static readonly Type[] MiscTypes =
    [
        typeof(KeyRing), typeof(Key),
        typeof(Scales), typeof(Spyglass), typeof(Lantern), typeof(HeatingStand),
        typeof(Globe), typeof(Candelabra)
    ];

    private static readonly Type[] NecklaceTypes = [typeof(GoldNecklace), typeof(SilverNecklace)];
    private static readonly Type[] EarringTypes = [typeof(GoldEarrings), typeof(SilverEarrings)];
    private static readonly Type[] RingTypes = [typeof(GoldRing), typeof(SilverRing), typeof(WeddingRing)];

    // Combined for AnyCraftableInCategory check on the Jewelry parent entry
    private static readonly Type[] AllJewelryTypes =
    [
        typeof(GoldNecklace), typeof(SilverNecklace),
        typeof(GoldEarrings), typeof(SilverEarrings),
        typeof(GoldRing), typeof(SilverRing), typeof(WeddingRing)
    ];

    private static readonly Type[] KegItemTypes = [typeof(PotionKeg)];

    private static ItemListEntry[] _mainEntries;
    private static ItemListEntry[] _woodEntries;
    private static ItemListEntry[] _toolEntries;
    private static ItemListEntry[] _partEntries;
    private static ItemListEntry[] _utensilEntries;
    private static ItemListEntry[] _trapEntries;
    private static ItemListEntry[] _miscEntries;
    private static ItemListEntry[] _necklaceEntries;
    private static ItemListEntry[] _earringEntries;
    private static ItemListEntry[] _ringEntries;
    private static ItemListEntry[] _kegEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;
    private readonly Type _selectedResourceType;

    private static string GetQuestion(Category category) => category switch
    {
        Category.Main      => "What would you like to make?",
        Category.Wood      => "What kind of wooden item?",
        Category.Tools     => "What kind of tool?",
        Category.Parts     => "What kind of part?",
        Category.Utensils  => "What kind of utensil?",
        Category.Traps     => "What kind of trap?",
        Category.Misc      => "What would you like to make?",
        Category.Jewelry   => "What kind of jewelry?",
        Category.Necklaces => "What kind of necklace?",
        Category.Earrings  => "What kind of earrings?",
        Category.Rings     => "What kind of ring?",
        Category.Keg       => "What would you like to make?",
        _                  => "What would you like to make?"
    };

    private TinkeringMenu(Mobile from, BaseTool tool, Category category, Type selectedResourceType)
        : base(GetQuestion(category), BuildFilteredEntries(from, category))
    {
        _tool = tool;
        _category = category;
        _selectedResourceType = selectedResourceType;
    }

    private static string FormatItemName(Type type)
    {
        var name = type.Name;
        Span<char> buffer = stackalloc char[name.Length * 2];
        var pos = 0;

        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                buffer[pos++] = ' ';
            }

            buffer[pos++] = char.ToLower(name[i]);
        }

        return new string(buffer[..pos]);
    }

    private static ItemListEntry[] BuildStaticEntries(Type[] types, string resourceName)
    {
        var entries = new ItemListEntry[types.Length];
        var count = 0;
        var craftItems = DefTinkering.CraftSystem.CraftItems;

        for (var i = 0; i < types.Length; i++)
        {
            var itemDef = craftItems.SearchFor(types[i]);
            if (itemDef == null)
            {
                continue;
            }

            var name = FormatItemName(types[i]);
            var res = itemDef.Resources[0];
            entries[count++] = new ItemListEntry($"{name} ({res.Amount} {resourceName})", itemDef.ItemId, 0, i);
        }

        if (count < entries.Length)
        {
            Array.Resize(ref entries, count);
        }

        return entries;
    }

    private static ItemListEntry[] GetStaticEntries(Category category) => category switch
    {
        Category.Main      => Main(),
        Category.Wood      => Wood(),
        Category.Tools     => Tools(),
        Category.Parts     => Parts(),
        Category.Utensils  => Utensils(),
        Category.Traps     => Traps(),
        Category.Misc      => Misc(),
        Category.Necklaces => Necklaces(),
        Category.Earrings  => Earrings(),
        Category.Rings     => Rings(),
        Category.Keg       => KegItems(),
        _                  => null
    };

    private static Type[] GetTypes(Category category) => category switch
    {
        Category.Wood      => WoodItemTypes,
        Category.Tools     => ToolTypes,
        Category.Parts     => PartTypes,
        Category.Utensils  => UtensilTypes,
        Category.Traps     => TrapTypes,
        Category.Misc      => MiscTypes,
        Category.Jewelry   => AllJewelryTypes,
        Category.Necklaces => NecklaceTypes,
        Category.Earrings  => EarringTypes,
        Category.Rings     => RingTypes,
        Category.Keg       => KegItemTypes,
        _                  => null
    };

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("Wooden Items", 0x1BDD, 0, (int)Category.Wood),
        new ItemListEntry("Tools", 0x1EB8, 0, (int)Category.Tools),
        new ItemListEntry("Parts", 0x1053, 0, (int)Category.Parts),
        new ItemListEntry("Utensils", 0x9D7, 0, (int)Category.Utensils),
        new ItemListEntry("Traps", 0x1BFC, 0, (int)Category.Traps),
        new ItemListEntry("Miscellaneous", 0xA25, 0, (int)Category.Misc),
        new ItemListEntry("Jewelry", 0x1088, 0, (int)Category.Jewelry)
    ];

    public static ItemListEntry[] Wood() => _woodEntries ??= BuildStaticEntries(WoodItemTypes, "logs");
    public static ItemListEntry[] Tools() => _toolEntries ??= BuildStaticEntries(ToolTypes, "ingots");
    public static ItemListEntry[] Parts() => _partEntries ??= BuildStaticEntries(PartTypes, "ingots");
    public static ItemListEntry[] Utensils() => _utensilEntries ??= BuildStaticEntries(UtensilTypes, "ingots");
    public static ItemListEntry[] Traps() => _trapEntries ??= BuildStaticEntries(TrapTypes, "ingots");
    public static ItemListEntry[] Misc() => _miscEntries ??= BuildStaticEntries(MiscTypes, "ingots");
    public static ItemListEntry[] Necklaces() => _necklaceEntries ??= BuildStaticEntries(NecklaceTypes, "ingots");
    public static ItemListEntry[] Earrings() => _earringEntries ??= BuildStaticEntries(EarringTypes, "ingots");
    public static ItemListEntry[] Rings() => _ringEntries ??= BuildStaticEntries(RingTypes, "ingots");
    public static ItemListEntry[] KegItems() => _kegEntries ??= BuildStaticEntries(KegItemTypes, "kegs");

    private static ItemListEntry[] BuildFilteredEntries(Mobile from, Category category)
    {
        if (category == Category.Main)
        {
            return BuildFilteredMainEntries(from);
        }

        if (category == Category.Jewelry)
        {
            return BuildFilteredJewelryEntries(from);
        }

        var types = GetTypes(category);
        var staticEntries = GetStaticEntries(category);
        if (types == null || staticEntries == null)
        {
            return [];
        }

        return T2ACraftSystem.FilterEntries(from, staticEntries, types, DefTinkering.CraftSystem);
    }

    private static readonly ItemListEntry[] JewelrySubcategoryEntries =
    [
        new("Necklaces", 0x1088, 0, (int)Category.Necklaces),
        new("Earrings", 0x1087, 0, (int)Category.Earrings),
        new("Rings", 0x108a, 0, (int)Category.Rings)
    ];

    private static ItemListEntry[] BuildFilteredJewelryEntries(Mobile from)
    {
        var system = DefTinkering.CraftSystem;
        var filtered = new ItemListEntry[JewelrySubcategoryEntries.Length];
        var count = 0;

        for (var i = 0; i < JewelrySubcategoryEntries.Length; i++)
        {
            var entry = JewelrySubcategoryEntries[i];
            var types = GetTypes((Category)entry.CraftIndex);
            if (types != null && T2ACraftSystem.AnyCraftableInCategory(from, types, system))
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

    private static ItemListEntry[] BuildFilteredMainEntries(Mobile from)
    {
        var system = DefTinkering.CraftSystem;
        var mainStatic = Main();
        var filtered = new ItemListEntry[mainStatic.Length];
        var count = 0;

        for (var i = 0; i < mainStatic.Length; i++)
        {
            var entry = mainStatic[i];
            var types = GetTypes((Category)entry.CraftIndex);
            if (types != null && T2ACraftSystem.AnyCraftableInCategory(from, types, system))
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

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = Entries[index].CraftIndex;

        // Navigation categories: Main → subcategory, Jewelry → subcategory
        if (_category is Category.Main or Category.Jewelry)
        {
            var childCategory = (Category)craftIndex;
            var resourceType = childCategory == Category.Wood ? typeof(Log) : typeof(IronIngot);
            var menu = new TinkeringMenu(from, _tool, childCategory, resourceType);
            if (menu.Entries.Length == 0)
            {
                from.SendAsciiMessage("You lack the skill and materials to craft anything in that category.");
                return;
            }

            from.SendMenu(menu);
            return;
        }

        // Jewelry leaf categories: prompt for gem targeting
        if (_category is Category.Necklaces or Category.Earrings or Category.Rings)
        {
            var types = GetTypes(_category);
            if (types == null || craftIndex < 0 || craftIndex >= types.Length)
            {
                return;
            }

            var itemType = types[craftIndex];
            if (DefTinkering.CraftSystem.CraftItems.SearchFor(itemType) == null)
            {
                return;
            }

            from.SendAsciiMessage("Target the gemstone you wish to use.");
            from.Target = new GemSelectTarget(from, _tool, itemType, _selectedResourceType);
            return;
        }

        // Leaf categories: craft directly
        var leafTypes = GetTypes(_category);
        if (leafTypes == null || craftIndex < 0 || craftIndex >= leafTypes.Length)
        {
            return;
        }

        var leafItemType = leafTypes[craftIndex];
        var itemDef = DefTinkering.CraftSystem.CraftItems.SearchFor(leafItemType);
        if (itemDef == null || _selectedResourceType == null)
        {
            return;
        }

        itemDef.Craft(from, DefTinkering.CraftSystem, _selectedResourceType, _tool);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResourceSelection(Mobile from, BaseTool tool) => from.Target = new ResourceSelectTarget(from, tool);

    private class ResourceSelectTarget : Target
    {
        private readonly Mobile _from;
        private readonly BaseTool _tool;

        public ResourceSelectTarget(Mobile from, BaseTool tool) : base(12, false, TargetFlags.None)
        {
            _from = from;
            _tool = tool;
            from.SendAsciiMessage("Select the resource you wish to use (log or iron ingot).");
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Log)
            {
                var menu = new TinkeringMenu(from, _tool, Category.Wood, typeof(Log));
                if (menu.Entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                    return;
                }

                from.SendMenu(menu);
            }
            else if (targeted is IronIngot)
            {
                // Show main menu for ingot selection (includes metal categories + Jewelry)
                var menu = new TinkeringMenu(from, _tool, Category.Main, typeof(IronIngot));
                if (menu.Entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                    return;
                }

                from.SendMenu(menu);
            }
            else if (targeted is Keg)
            {
                var menu = new TinkeringMenu(from, _tool, Category.Keg, typeof(Keg));
                if (menu.Entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                    return;
                }

                from.SendMenu(menu);
            }
            else
            {
                from.SendAsciiMessage("That is not a valid resource. Please select log or iron ingot.");
                from.Target = new ResourceSelectTarget(_from, _tool);
            }
        }
    }

    private class GemSelectTarget : Target
    {
        private readonly Mobile _from;
        private readonly BaseTool _tool;
        private readonly Type _itemType;
        private readonly Type _selectedResourceType;

        public GemSelectTarget(Mobile from, BaseTool tool, Type itemType, Type selectedResourceType)
            : base(12, false, TargetFlags.None)
        {
            _from = from;
            _tool = tool;
            _itemType = itemType;
            _selectedResourceType = selectedResourceType;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Item gemItem)
            {
                from.SendAsciiMessage("That is not a gemstone.");
                return;
            }

            var gemType = BaseJewel.GetGemType(gemItem);
            if (gemType == GemType.None)
            {
                from.SendAsciiMessage("That is not a gemstone.");
                return;
            }

            var amount = gemItem.Amount;
            if (amount < 1)
            {
                from.SendAsciiMessage("That gemstone stack is empty.");
                return;
            }

            var system = DefTinkering.CraftSystem;
            var itemDef = system.CraftItems.SearchFor(_itemType);
            if (itemDef == null)
            {
                return;
            }

            // Store pending gem info in craft context
            var ctx = system.GetContext(from);
            if (ctx == null)
            {
                return;
            }

            ctx.PendingGemType = gemType;
            ctx.PendingGemCount = amount;

            itemDef.Craft(from, system, _selectedResourceType, _tool);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            if (cancelType == TargetCancelType.Canceled)
            {
                CraftItem.ShowCraftMenu(from, DefTinkering.CraftSystem, _tool);
            }
        }
    }
}
