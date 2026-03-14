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
        typeof(ChainLegs), typeof(ChainChest)
    ];

    private static readonly Type[] PlatemailTypes =
    [
        typeof(PlateArms), typeof(PlateGloves), typeof(PlateGorget), typeof(PlateLegs),
        typeof(PlateChest), typeof(FemalePlateChest)
    ];

    private static readonly Type[] HelmetTypes =
    [
        typeof(ChainCoif), typeof(Bascinet), typeof(CloseHelm), typeof(Helmet),
        typeof(NorseHelm), typeof(PlateHelm)
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
    private static ItemListEntry[] _helmetEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;

    public BlacksmithMenu(BaseTool tool) : this(tool, Category.Main)
    {
    }

    private BlacksmithMenu(BaseTool tool, Category category)
        : base("Choose an item.", GetEntries(category))
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

    private static ItemListEntry[] GetEntries(Category category) => category switch
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
        Category.Helmets   => Helmets(),
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
        Category.Helmets   => HelmetTypes,
        _                  => null
    };

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("Repair", 4015),
        new ItemListEntry("Shields", 7026, 0, (int)Category.Shields),
        new ItemListEntry("Armor", 5141, 0, (int)Category.Armor),
        new ItemListEntry("Weapons", 5049, 0, (int)Category.Weapons)
    ];

    public static ItemListEntry[] Weapons() => _weaponEntries ??=
    [
        new ItemListEntry("Swords & Blades", 5049, 0, (int)Category.Blades),
        new ItemListEntry("Axes", 3913, 0, (int)Category.Axes),
        new ItemListEntry("Maces & Hammers", 5127, 0, (int)Category.Maces),
        new ItemListEntry("Polearms", 3917, 0, (int)Category.Polearms)
    ];

    public static ItemListEntry[] Armor() => _armorEntries ??=
    [
        new ItemListEntry("Platemail", 5141, 0, (int)Category.Platemail),
        new ItemListEntry("Chainmail", 5055, 0, (int)Category.Chainmail),
        new ItemListEntry("Ringmail", 5100, 0, (int)Category.Ringmail),
        new ItemListEntry("Helmets", 5138, 0, (int)Category.Helmets)
    ];

    public static ItemListEntry[] Shields() => _shieldEntries ??= BuildStaticEntries(ShieldTypes, "ingots");
    public static ItemListEntry[] Blades() => _bladeEntries ??= BuildStaticEntries(BladeTypes, "ingots");
    public static ItemListEntry[] Axes() => _axeEntries ??= BuildStaticEntries(AxeTypes, "ingots");
    public static ItemListEntry[] Maces() => _maceEntries ??= BuildStaticEntries(MaceTypes, "ingots");
    public static ItemListEntry[] Polearms() => _polearmEntries ??= BuildStaticEntries(PolearmTypes, "ingots");
    public static ItemListEntry[] Platemail() => _platemailEntries ??= BuildStaticEntries(PlatemailTypes, "ingots");
    public static ItemListEntry[] Chainmail() => _chainmailEntries ??= BuildStaticEntries(ChainmailTypes, "ingots");
    public static ItemListEntry[] Ringmail() => _ringmailEntries ??= BuildStaticEntries(RingmailTypes, "ingots");
    public static ItemListEntry[] Helmets() => _helmetEntries ??= BuildStaticEntries(HelmetTypes, "ingots");

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
        var craftIndex = GetEntries(_category)[index].CraftIndex;

        if (_category == Category.Main)
        {
            if (craftIndex == 0)
            {
                Repair.Do(from, DefBlacksmithy.CraftSystem, _tool);
                return;
            }

            SendSubMenu(from, (Category)craftIndex);
            return;
        }

        if (_category == Category.Weapons || _category == Category.Armor)
        {
            from.SendMenu(new BlacksmithMenu(_tool, (Category)craftIndex));
            return;
        }

        var types = GetTypes(_category);
        if (types != null && craftIndex >= 0 && craftIndex < types.Length)
        {
            CraftItemFromType(from, types[craftIndex]);
        }
    }

    private void SendSubMenu(Mobile from, Category category)
    {
        var context = DefBlacksmithy.CraftSystem.GetContext(from);
        if (context == null || context.LastResourceIndex < 0)
        {
            ResourceSelection(from, _tool, (mob, tool) =>
                mob.SendMenu(new BlacksmithMenu(tool, category)));
        }
        else
        {
            from.SendMenu(new BlacksmithMenu(_tool, category));
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
