using System;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.Engines.Craft.T2A;

public class CarpentryMenu : ItemListMenu
{
    private enum Category
    {
        Main,
        Furniture,
        Containers,
        Weapons,
        Instruments,
        Misc,
        Addons
    }

    private static readonly Type[] FurnitureTypes =
    [
        typeof(FootStool), typeof(Stool), typeof(BambooChair), typeof(WoodenChair),
        typeof(FancyWoodenChairCushion), typeof(WoodenChairCushion),
        typeof(WoodenBench), typeof(WoodenThrone), typeof(Throne),
        typeof(Nightstand), typeof(WritingTable), typeof(YewWoodTable), typeof(LargeTable)
    ];

    private static readonly Type[] ContainerTypes =
    [
        typeof(WoodenBox), typeof(SmallCrate), typeof(MediumCrate), typeof(LargeCrate),
        typeof(WoodenChest), typeof(EmptyBookcase), typeof(FancyArmoire), typeof(Armoire),
        typeof(Keg)
    ];

    private static readonly Type[] WeaponTypes =
    [
        typeof(ShepherdsCrook), typeof(QuarterStaff), typeof(GnarledStaff), typeof(WoodenShield)
    ];

    private static readonly Type[] InstrumentTypes =
    [
        typeof(LapHarp), typeof(Harp), typeof(Drums), typeof(Lute),
        typeof(Tambourine), typeof(TambourineTassel)
    ];

    private static readonly Type[] MiscItemTypes =
    [
        typeof(FishingPole), typeof(BarrelStaves), typeof(BarrelLid),
        typeof(ShortMusicStand), typeof(TallMusicStand), typeof(Easel)
    ];

    private static readonly Type[] AddonTypes =
    [
        typeof(SmallBedSouthDeed), typeof(SmallBedEastDeed),
        typeof(LargeBedSouthDeed), typeof(LargeBedEastDeed),
        typeof(DartBoardSouthDeed), typeof(DartBoardEastDeed),
        typeof(BallotBoxDeed),
        typeof(PentagramDeed), typeof(AbbatoirDeed),
        typeof(SmallForgeDeed), typeof(LargeForgeEastDeed), typeof(LargeForgeSouthDeed),
        typeof(AnvilEastDeed), typeof(AnvilSouthDeed),
        typeof(TrainingDummyEastDeed), typeof(TrainingDummySouthDeed),
        typeof(PickpocketDipEastDeed), typeof(PickpocketDipSouthDeed),
        typeof(Dressform),
        typeof(SpinningWheelEastDeed), typeof(SpinningWheelSouthDeed),
        typeof(LoomEastDeed), typeof(LoomSouthDeed),
        typeof(StoneOvenEastDeed), typeof(StoneOvenSouthDeed),
        typeof(FlourMillEastDeed), typeof(FlourMillSouthDeed),
        typeof(WaterTroughEastDeed), typeof(WaterTroughSouthDeed)
    ];

    private static ItemListEntry[] _mainEntries;
    private static ItemListEntry[] _furnitureEntries;
    private static ItemListEntry[] _containerEntries;
    private static ItemListEntry[] _weaponEntries;
    private static ItemListEntry[] _instrumentEntries;
    private static ItemListEntry[] _miscEntries;
    private static ItemListEntry[] _addonEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;

    public CarpentryMenu(Mobile from, BaseTool tool) : this(from, tool, Category.Main)
    {
    }

    private static string GetQuestion(Category category) => category switch
    {
        Category.Main        => "What would you like to make?",
        Category.Furniture   => "What kind of furniture?",
        Category.Containers  => "What kind of container?",
        Category.Weapons     => "What kind of weapon?",
        Category.Instruments => "What kind of instrument?",
        Category.Addons      => "What kind of add-on?",
        _                    => "What would you like to make?"
    };

    private CarpentryMenu(Mobile from, BaseTool tool, Category category)
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
        var craftItems = DefCarpentry.CraftSystem.CraftItems;

        for (var i = 0; i < types.Length; i++)
        {
            var itemDef = craftItems.SearchFor(types[i]);
            if (itemDef == null)
            {
                continue;
            }

            var name = FormatItemName(types[i]);
            var res = itemDef.Resources;

            string label;
            if (res.Count > 1)
            {
                var secondName = res[1].ItemType == typeof(IronIngot) ? "ingots" : "cloth";
                label = $"{name} ({res[0].Amount} {resourceName}, {res[1].Amount} {secondName})";
            }
            else
            {
                label = $"{name} ({res[0].Amount} {resourceName})";
            }

            entries[count++] = new ItemListEntry(label, itemDef.ItemId, 0, i);
        }

        if (count < entries.Length)
        {
            Array.Resize(ref entries, count);
        }

        return entries;
    }

    private static ItemListEntry[] GetStaticEntries(Category category) => category switch
    {
        Category.Main        => Main(),
        Category.Furniture   => Furniture(),
        Category.Containers  => Containers(),
        Category.Weapons     => Weapons(),
        Category.Instruments => Instruments(),
        Category.Misc        => Misc(),
        Category.Addons      => Addons(),
        _                    => null
    };

    private static Type[] GetTypes(Category category) => category switch
    {
        Category.Furniture   => FurnitureTypes,
        Category.Containers  => ContainerTypes,
        Category.Weapons     => WeaponTypes,
        Category.Instruments => InstrumentTypes,
        Category.Misc        => MiscItemTypes,
        Category.Addons      => AddonTypes,
        _                    => null
    };

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("Furniture", 0xB57, 0, (int)Category.Furniture),
        new ItemListEntry("Containers", 0x9AA, 0, (int)Category.Containers),
        new ItemListEntry("Weapons", 0xE89, 0, (int)Category.Weapons),
        new ItemListEntry("Instruments", 0xEB3, 0, (int)Category.Instruments),
        new ItemListEntry("Miscellaneous", 0xDC0, 0, (int)Category.Misc),
        new ItemListEntry("Add-Ons", 0x14F0, 0, (int)Category.Addons)
    ];

    public static ItemListEntry[] Furniture() => _furnitureEntries ??= BuildStaticEntries(FurnitureTypes, "wood");
    public static ItemListEntry[] Containers() => _containerEntries ??= BuildStaticEntries(ContainerTypes, "wood");
    public static ItemListEntry[] Weapons() => _weaponEntries ??= BuildStaticEntries(WeaponTypes, "wood");
    public static ItemListEntry[] Instruments() => _instrumentEntries ??= BuildStaticEntries(InstrumentTypes, "wood");
    public static ItemListEntry[] Misc() => _miscEntries ??= BuildStaticEntries(MiscItemTypes, "wood");
    public static ItemListEntry[] Addons() => _addonEntries ??= BuildStaticEntries(AddonTypes, "wood");

    private static ItemListEntry[] BuildFilteredEntries(Mobile from, Category category)
    {
        if (category == Category.Main)
        {
            return BuildFilteredMainEntries(from);
        }

        var types = GetTypes(category);
        var staticEntries = GetStaticEntries(category);
        if (types == null || staticEntries == null)
        {
            return [];
        }

        return T2ACraftSystem.FilterEntries(from, staticEntries, types, DefCarpentry.CraftSystem);
    }

    private static ItemListEntry[] BuildFilteredMainEntries(Mobile from)
    {
        var system = DefCarpentry.CraftSystem;
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

    private void CraftSelectedItem(Mobile from, Type itemType)
    {
        var itemDef = DefCarpentry.CraftSystem.CraftItems.SearchFor(itemType);

        if (itemDef == null)
        {
            return;
        }

        var num = DefCarpentry.CraftSystem.CanCraft(from, _tool, itemDef.ItemType);

        if (num > 0)
        {
            from.SendLocalizedMessage(num);
            return;
        }

        var context = DefCarpentry.CraftSystem.GetContext(from);
        var res = itemDef.UseSubRes2 ? DefCarpentry.CraftSystem.CraftSubRes2 : DefCarpentry.CraftSystem.CraftSubRes;
        var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        var type = resIndex > -1 ? res[resIndex].ItemType : null;
        DefCarpentry.CraftSystem.CreateItem(from, itemDef.ItemType, type, _tool, itemDef);
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = Entries[index].CraftIndex;

        if (_category == Category.Main)
        {
            var menu = new CarpentryMenu(from, _tool, (Category)craftIndex);
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
            CraftSelectedItem(from, types[craftIndex]);
        }
    }
}
