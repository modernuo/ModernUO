using System;
using System.Collections.Generic;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.Engines.Craft.T2A;

public class CarpentryMenu : ItemListMenu
{
    private enum Category
    {
        Main,
        Chairs,
        Tables,
        Misc
    }

    private static readonly Type[] ChairTypes =
    [
        typeof(FootStool), typeof(Stool), typeof(BambooChair), typeof(WoodenChair),
        typeof(FancyWoodenChairCushion), typeof(WoodenChairCushion),
        typeof(WoodenBench), typeof(WoodenThrone), typeof(Throne)
    ];

    private static readonly Type[] TableTypes =
    [
        typeof(Nightstand), typeof(WritingTable), typeof(YewWoodTable), typeof(LargeTable)
    ];

    private static readonly Type[] ContainerTypes =
    [
        typeof(WoodenBox), typeof(SmallCrate), typeof(MediumCrate), typeof(LargeCrate),
        typeof(WoodenChest), typeof(EmptyBookcase), typeof(FancyArmoire), typeof(Armoire),
        typeof(Keg)
    ];

    private static readonly Type[] MiscItemTypes =
    [
        typeof(ShepherdsCrook), typeof(QuarterStaff), typeof(GnarledStaff),
        typeof(FishingPole), typeof(WoodenShield)
    ];

    private static Type[] _allMiscTypes;

    private static ItemListEntry[] _mainEntries;
    private static ItemListEntry[] _chairEntries;
    private static ItemListEntry[] _tableEntries;
    private static ItemListEntry[] _miscEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;

    public CarpentryMenu(BaseTool tool) : this(tool, Category.Main)
    {
    }

    private CarpentryMenu(BaseTool tool, Category category)
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

            var label = res.Count > 1
                ? $"{name} ({res[0].Amount} {resourceName}, {res[1].Amount} cloth)"
                : $"{name} ({res[0].Amount} {resourceName})";

            entries[count++] = new ItemListEntry(label, itemDef.ItemId, 0, i);
        }

        if (count < entries.Length)
        {
            Array.Resize(ref entries, count);
        }

        return entries;
    }

    private static Type[] BuildMiscTypes()
    {
        var craftItems = DefCarpentry.CraftSystem.CraftItems;
        var allTypes = new List<Type>();

        for (var i = 0; i < ContainerTypes.Length; i++)
        {
            var type = ContainerTypes[i];
            if (craftItems.SearchFor(type) != null)
            {
                allTypes.Add(type);
            }
        }

        for (var i = 0; i < MiscItemTypes.Length; i++)
        {
            var type = MiscItemTypes[i];
            if (craftItems.SearchFor(type) != null)
            {
                allTypes.Add(type);
            }
        }

        return allTypes.ToArray();
    }

    private static Type[] GetAllMiscTypes() => _allMiscTypes ??= BuildMiscTypes();

    private static ItemListEntry[] GetEntries(Category category) => category switch
    {
        Category.Main   => Main(),
        Category.Chairs => Chairs(),
        Category.Tables => Tables(),
        Category.Misc   => Misc(),
        _               => null
    };

    private static Type[] GetTypes(Category category) => category switch
    {
        Category.Chairs => ChairTypes,
        Category.Tables => TableTypes,
        Category.Misc   => GetAllMiscTypes(),
        _               => null
    };

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("Chairs", 2902, 0, (int)Category.Chairs),
        new ItemListEntry("Tables", 2940, 0, (int)Category.Tables),
        new ItemListEntry("Miscellaneous", 3650, 0, (int)Category.Misc)
    ];

    public static ItemListEntry[] Chairs() => _chairEntries ??= BuildStaticEntries(ChairTypes, "wood");
    public static ItemListEntry[] Tables() => _tableEntries ??= BuildStaticEntries(TableTypes, "wood");
    public static ItemListEntry[] Misc() => _miscEntries ??= BuildStaticEntries(GetAllMiscTypes(), "wood");

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
        var craftIndex = GetEntries(_category)[index].CraftIndex;

        if (_category == Category.Main)
        {
            from.SendMenu(new CarpentryMenu(_tool, (Category)craftIndex));
            return;
        }

        var types = GetTypes(_category);
        if (types != null && craftIndex >= 0 && craftIndex < types.Length)
        {
            CraftSelectedItem(from, types[craftIndex]);
        }
    }
}
