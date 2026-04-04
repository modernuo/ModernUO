using System;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

public class BlacksmithMenu : ItemListMenu
{
    private enum Category
    {
        Main,
        Shields,
        Weapons,
        Armor,
        Blades,
        Axes,
        Maces,
        Polearms,
        Platemail,
        Chainmail,
        Ringmail,
        Helmets
    }

    private static readonly Type[] RingmailTypes =
    [
        typeof(RingmailGloves), typeof(RingmailLegs), typeof(RingmailArms), typeof(RingmailChest)
    ];

    private static readonly Type[] ChainmailTypes =
    [
        typeof(ChainCoif), typeof(ChainLegs), typeof(ChainChest)
    ];

    private static readonly Type[] PlatemailTypes =
    [
        typeof(Bascinet), typeof(CloseHelm), typeof(Helmet), typeof(NorseHelm), typeof(PlateHelm),
        typeof(PlateArms), typeof(PlateGloves), typeof(PlateGorget), typeof(PlateLegs),
        typeof(PlateChest), typeof(FemalePlateChest)
    ];

    private static readonly Type[] ShieldTypes =
    [
        typeof(Buckler), typeof(BronzeShield), typeof(HeaterShield), typeof(MetalShield),
        typeof(MetalKiteShield), typeof(WoodenKiteShield)
    ];

    private static readonly Type[] BladeTypes =
    [
        typeof(Broadsword), typeof(Cutlass), typeof(Dagger), typeof(Katana),
        typeof(Kryss), typeof(Longsword), typeof(Scimitar), typeof(VikingSword)
    ];

    private static readonly Type[] AxeTypes =
    [
        typeof(Axe), typeof(BattleAxe), typeof(DoubleAxe), typeof(ExecutionersAxe),
        typeof(LargeBattleAxe), typeof(TwoHandedAxe), typeof(WarAxe)
    ];

    private static readonly Type[] PolearmTypes =
    [
        typeof(Bardiche), typeof(Halberd), typeof(ShortSpear), typeof(Spear)
    ];

    private static readonly Type[] MaceTypes =
    [
        typeof(HammerPick), typeof(Mace), typeof(Maul), typeof(WarMace), typeof(WarHammer)
    ];

    // Non-category actions at the top of the main menu (Repair, Smelt).
    // Category CraftIndex values are offset by this count.
    private const int MainActionCount = 2;

    private static ItemListEntry[] _mainEntries;
    private static ItemListEntry[] _weaponEntries;
    private static ItemListEntry[] _armorEntries;
    private static ItemListEntry[] _shieldEntries;
    private static ItemListEntry[] _bladeEntries;
    private static ItemListEntry[] _axeEntries;
    private static ItemListEntry[] _maceEntries;
    private static ItemListEntry[] _polearmEntries;
    private static ItemListEntry[] _platemailEntries;
    private static ItemListEntry[] _chainmailEntries;
    private static ItemListEntry[] _ringmailEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;

    // Resource is always selected before any menu is shown (Lost Lands flow)
    public BlacksmithMenu(Mobile from, BaseTool tool) : this(from, tool, Category.Main)
    {
    }

    private static string GetQuestion(Category category) => category switch
    {
        Category.Main      => "What would you like to do?",
        Category.Armor     => "What kind of armor?",
        Category.Shields   => "What kind of shield?",
        Category.Weapons   => "What kind of weapon?",
        Category.Blades    => "What kind of blade?",
        Category.Axes      => "What kind of axe?",
        Category.Polearms  => "What kind of pole arm?",
        Category.Maces     => "What kind of bludgeoning weapon?",
        Category.Ringmail  => "What kind of ring armor?",
        Category.Chainmail => "What kind of chain armor?",
        Category.Platemail => "What kind of plate armor?",
        _                  => "What would you like to make?"
    };

    private BlacksmithMenu(Mobile from, BaseTool tool, Category category)
        : base(GetQuestion(category), BuildFilteredEntries(from, category))
    {
        _tool = tool;
        _category = category;
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
        var craftItems = DefBlacksmithy.CraftSystem.CraftItems;

        for (var i = 0; i < types.Length; i++)
        {
            var itemDef = craftItems.SearchFor(types[i]);
            if (itemDef == null)
            {
                continue;
            }

            var name = FormatItemName(types[i]);
            var res = itemDef.Resources[0];
            var itemId = itemDef.ItemId;

            if (itemId == 7033)
            {
                itemId = 7032;
            }

            entries[count++] = new ItemListEntry($"{name} ({res.Amount} {resourceName})", itemId, 0, i);
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
        Category.Weapons   => Weapons(),
        Category.Armor     => Armor(),
        Category.Shields   => Shields(),
        Category.Blades    => Blades(),
        Category.Axes      => Axes(),
        Category.Maces     => Maces(),
        Category.Polearms  => Polearms(),
        Category.Platemail => Platemail(),
        Category.Chainmail => Chainmail(),
        Category.Ringmail  => Ringmail(),
        _                  => null
    };

    private static Type[] GetTypes(Category category) => category switch
    {
        Category.Shields   => ShieldTypes,
        Category.Blades    => BladeTypes,
        Category.Axes      => AxeTypes,
        Category.Maces     => MaceTypes,
        Category.Polearms  => PolearmTypes,
        Category.Platemail => PlatemailTypes,
        Category.Chainmail => ChainmailTypes,
        Category.Ringmail  => RingmailTypes,
        _                  => null
    };

    // Leaf categories under Weapons
    private static readonly Category[] WeaponLeafCategories =
    [
        Category.Blades, Category.Axes, Category.Maces, Category.Polearms
    ];

    // Leaf categories under Armor
    private static readonly Category[] ArmorLeafCategories =
    [
        Category.Ringmail, Category.Chainmail, Category.Platemail
    ];

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("Repair", 0x0FAF, 0, 0),
        new ItemListEntry("Smelt", 0x0FB1, 0, 1),
        new ItemListEntry("Build Armor", 0x13EC, 0, (int)Category.Armor + MainActionCount),
        new ItemListEntry("Build Shield", 0x1B74, 0, (int)Category.Shields + MainActionCount),
        new ItemListEntry("Build Weapons", 0xF45, 0, (int)Category.Weapons + MainActionCount)
    ];

    public static ItemListEntry[] Weapons() => _weaponEntries ??=
    [
        new ItemListEntry("Build Blades", 0xF61, 0, (int)Category.Blades),
        new ItemListEntry("Build Axes", 0x13FB, 0, (int)Category.Axes),
        new ItemListEntry("Build Pole Arms", 0xF4D, 0, (int)Category.Polearms),
        new ItemListEntry("Build Bludgeoning Weapons", 0x1407, 0, (int)Category.Maces)
    ];

    public static ItemListEntry[] Armor() => _armorEntries ??=
    [
        new ItemListEntry("Build Ring Armor", 0x13EC, 0, (int)Category.Ringmail),
        new ItemListEntry("Build Chain Armor", 0x13BF, 0, (int)Category.Chainmail),
        new ItemListEntry("Build Plate Armor", 0x1415, 0, (int)Category.Platemail)
    ];

    public static ItemListEntry[] Shields() => _shieldEntries ??= BuildStaticEntries(ShieldTypes, "ingots");
    public static ItemListEntry[] Blades() => _bladeEntries ??= BuildStaticEntries(BladeTypes, "ingots");
    public static ItemListEntry[] Axes() => _axeEntries ??= BuildStaticEntries(AxeTypes, "ingots");
    public static ItemListEntry[] Maces() => _maceEntries ??= BuildStaticEntries(MaceTypes, "ingots");
    public static ItemListEntry[] Polearms() => _polearmEntries ??= BuildStaticEntries(PolearmTypes, "ingots");
    public static ItemListEntry[] Platemail() => _platemailEntries ??= BuildStaticEntries(PlatemailTypes, "ingots");
    public static ItemListEntry[] Chainmail() => _chainmailEntries ??= BuildStaticEntries(ChainmailTypes, "ingots");
    public static ItemListEntry[] Ringmail() => _ringmailEntries ??= BuildStaticEntries(RingmailTypes, "ingots");

    private static Type GetSelectedResourceType(Mobile from)
    {
        var context = DefBlacksmithy.CraftSystem.GetContext(from);
        if (context?.LastResourceIndex >= 0)
        {
            var res = DefBlacksmithy.CraftSystem.CraftSubRes;
            if (context.LastResourceIndex < res.Count)
            {
                return res[context.LastResourceIndex].ItemType;
            }
        }

        return null;
    }

    private static ItemListEntry[] BuildFilteredEntries(Mobile from, Category category)
    {
        // Resource is always selected before any menu is shown
        var selectedResType = GetSelectedResourceType(from);

        if (category == Category.Main)
        {
            return BuildFilteredMainEntries(from, selectedResType);
        }

        if (category == Category.Weapons)
        {
            return BuildFilteredMidEntries(from, Weapons(), WeaponLeafCategories, selectedResType);
        }

        if (category == Category.Armor)
        {
            return BuildFilteredMidEntries(from, Armor(), ArmorLeafCategories, selectedResType);
        }

        // Leaf category: filter individual items
        var types = GetTypes(category);
        var staticEntries = GetStaticEntries(category);
        if (types == null || staticEntries == null)
        {
            return [];
        }

        return T2ACraftSystem.FilterEntries(from, staticEntries, types, DefBlacksmithy.CraftSystem, selectedResType);
    }

    private static ItemListEntry[] BuildFilteredMainEntries(Mobile from, Type selectedResType)
    {
        var system = DefBlacksmithy.CraftSystem;
        var mainStatic = Main();
        var filtered = new ItemListEntry[mainStatic.Length];
        var count = 0;

        for (var i = 0; i < mainStatic.Length; i++)
        {
            var entry = mainStatic[i];

            // Repair and Smelt are always shown
            if (entry.CraftIndex < MainActionCount)
            {
                filtered[count++] = entry;
                continue;
            }

            var cat = (Category)(entry.CraftIndex - MainActionCount);

            if (cat == Category.Shields)
            {
                if (T2ACraftSystem.AnyCraftableInCategory(from, ShieldTypes, system, selectedResType))
                {
                    filtered[count++] = entry;
                }
            }
            else if (cat == Category.Weapons)
            {
                if (AnyLeafCraftable(from, system, WeaponLeafCategories, selectedResType))
                {
                    filtered[count++] = entry;
                }
            }
            else if (cat == Category.Armor)
            {
                if (AnyLeafCraftable(from, system, ArmorLeafCategories, selectedResType))
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

    private static ItemListEntry[] BuildFilteredMidEntries(
        Mobile from, ItemListEntry[] staticEntries, Category[] leafCategories, Type selectedResType
    )
    {
        var system = DefBlacksmithy.CraftSystem;
        var filtered = new ItemListEntry[staticEntries.Length];
        var count = 0;

        for (var i = 0; i < staticEntries.Length; i++)
        {
            var entry = staticEntries[i];
            var leafCat = (Category)entry.CraftIndex;
            var types = GetTypes(leafCat);
            if (types != null && T2ACraftSystem.AnyCraftableInCategory(from, types, system, selectedResType))
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

    private static bool AnyLeafCraftable(
        Mobile from, CraftSystem system, Category[] leafCategories, Type selectedResType = null
    )
    {
        for (var i = 0; i < leafCategories.Length; i++)
        {
            var types = GetTypes(leafCategories[i]);
            if (types != null && T2ACraftSystem.AnyCraftableInCategory(from, types, system, selectedResType))
            {
                return true;
            }
        }

        return false;
    }

    private void CraftItemFromType(Mobile from, Type itemType)
    {
        var itemDef = DefBlacksmithy.CraftSystem.CraftItems.SearchFor(itemType);

        if (itemDef == null)
        {
            return;
        }

        var num = DefBlacksmithy.CraftSystem.CanCraft(from, _tool, itemDef.ItemType);
        if (num > 0)
        {
            from.SendLocalizedMessage(num);
            return;
        }

        var context = DefBlacksmithy.CraftSystem.GetContext(from);
        var res = itemDef.UseSubRes2
            ? DefBlacksmithy.CraftSystem.CraftSubRes2
            : DefBlacksmithy.CraftSystem.CraftSubRes;
        var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        var type = resIndex > -1 ? res[resIndex].ItemType : null;

        DefBlacksmithy.CraftSystem.CreateItem(from, itemDef.ItemType, type, _tool, itemDef);
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = Entries[index].CraftIndex;

        if (_category == Category.Main)
        {
            switch (craftIndex)
            {
                case 0:
                    Repair.Do(from, DefBlacksmithy.CraftSystem, _tool);
                    return;
                case 1:
                    Resmelt.Do(from, DefBlacksmithy.CraftSystem, _tool);
                    return;
            }

            var menu = new BlacksmithMenu(from, _tool, (Category)(craftIndex - MainActionCount));
            if (menu.Entries.Length == 0)
            {
                from.SendAsciiMessage("You lack the skill and materials to craft anything in that category.");
                return;
            }

            from.SendMenu(menu);
            return;
        }

        if (_category is Category.Weapons or Category.Armor)
        {
            var menu = new BlacksmithMenu(from, _tool, (Category)craftIndex);
            if (menu.Entries.Length == 0)
            {
                from.SendAsciiMessage("You lack the skill and materials to craft anything in that category.");
                return;
            }

            from.SendMenu(menu);
            return;
        }

        var types = GetTypes(_category);
        if (types != null && craftIndex >= 0 && craftIndex < types.Length)
        {
            CraftItemFromType(from, types[craftIndex]);
        }
    }

    public override void OnCancel(NetState state)
    {
        base.OnCancel(state);
        var context = DefBlacksmithy.CraftSystem.GetContext(state.Mobile);
        context?.LastResourceIndex = -1;
    }

    public static void ResourceSelection(Mobile from, BaseTool tool, Action<Mobile, BaseTool> afterSelect)
    {
        var res = DefBlacksmithy.CraftSystem.CraftSubRes;
        var availableCount = 0;
        var lastAvailable = -1;

        for (var i = 0; i < res.Count; ++i)
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
            {
                context.LastResourceIndex = lastAvailable;
            }

            afterSelect(from, tool);
        }
        else
        {
            from.SendMessage("Target the ingots you wish to use.");
            from.Target = new BlacksmithResourceTarget(tool, afterSelect);
        }
    }
}

public class BlacksmithResourceTarget : Target
{
    private readonly BaseTool _tool;
    private readonly Action<Mobile, BaseTool> _afterSelect;

    public BlacksmithResourceTarget(BaseTool tool, Action<Mobile, BaseTool> afterSelect)
        : base(2, false, TargetFlags.None)
    {
        _tool = tool;
        _afterSelect = afterSelect;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is Item item)
        {
            var res = DefBlacksmithy.CraftSystem.CraftSubRes;
            for (var i = 0; i < res.Count; ++i)
            {
                if (item.GetType() == res[i].ItemType)
                {
                    var context = DefBlacksmithy.CraftSystem.GetContext(from);
                    context?.LastResourceIndex = i;

                    _afterSelect(from, _tool);
                    return;
                }
            }
        }

        from.SendMessage("That is not a valid ingot.");
        from.Target = new BlacksmithResourceTarget(_tool, _afterSelect);
    }
}
