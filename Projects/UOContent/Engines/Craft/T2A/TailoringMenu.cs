using System;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.Craft.T2A;

public class TailoringMenu : ItemListMenu
{
    private enum Category
    {
        Main,
        LeatherMain,
        Shirts,
        Pants,
        Misc,
        Footwear,
        Leather,
        Studded,
        Female
    }

    private static readonly Type[] ShirtsTypes =
    [
        typeof(Doublet), typeof(Shirt), typeof(FancyShirt), typeof(Tunic), typeof(Surcoat), typeof(PlainDress)
    ];

    private static readonly Type[] PantsTypes = [typeof(ShortPants), typeof(LongPants), typeof(Kilt) ];

    private static readonly Type[] MiscTypes =
    [
        typeof(Skirt), typeof(Cloak), typeof(Robe), typeof(JesterSuit), typeof(FancyDress)
    ];

    private static readonly Type[] FootwearTypes = [typeof(Sandals), typeof(Shoes), typeof(Boots), typeof(ThighBoots)];

    private static readonly Type[] LeatherArmorTypes =
    [
        typeof(LeatherChest), typeof(LeatherGorget), typeof(LeatherGloves), typeof(LeatherCap),
        typeof(LeatherArms), typeof(LeatherLegs)
    ];

    private static readonly Type[] StuddedArmorTypes =
    [
        typeof(StuddedChest), typeof(StuddedGorget), typeof(StuddedGloves), typeof(StuddedArms), typeof(StuddedLegs)
    ];

    private static readonly Type[] FemaleArmorTypes =
    [
        typeof(FemaleLeatherChest), typeof(FemaleStuddedChest), typeof(LeatherBustierArms), typeof(StuddedBustierArms),
        typeof(FemalePlateChest), typeof(LeatherShorts), typeof(LeatherSkirt)
    ];

    private static ItemListEntry[] _mainEntries;
    private static ItemListEntry[] _leatherMainEntries;
    private static ItemListEntry[] _shirtsEntries;
    private static ItemListEntry[] _pantsEntries;
    private static ItemListEntry[] _miscEntries;
    private static ItemListEntry[] _footwearEntries;
    private static ItemListEntry[] _leatherEntries;
    private static ItemListEntry[] _studdedEntries;
    private static ItemListEntry[] _femaleEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;
    private readonly int _hue;

    private static string GetQuestion(Category category) => category switch
    {
        Category.Main        => "What would you like to make?",
        Category.LeatherMain => "What would you like to make?",
        Category.Shirts      => "What kind of shirt?",
        Category.Pants       => "What kind of pants?",
        Category.Misc        => "What would you like to make?",
        Category.Footwear    => "What kind of footwear?",
        Category.Leather     => "What kind of leather armor?",
        Category.Studded     => "What kind of studded armor?",
        Category.Female      => "What kind of female leather?",
        _                    => "What would you like to make?"
    };

    private TailoringMenu(Mobile from, BaseTool tool, Category category, int hue = -1)
        : base(GetQuestion(category), BuildFilteredEntries(from, category))
    {
        _tool = tool;
        _category = category;
        _hue = hue;
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
        var craftItems = DefTailoring.CraftSystem.CraftItems;

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
        Category.Main        => Main(),
        Category.LeatherMain => LeatherMain(),
        Category.Shirts      => Shirts(),
        Category.Pants       => Pants(),
        Category.Misc        => Misc(),
        Category.Footwear    => Footwear(),
        Category.Leather     => Leather(),
        Category.Studded     => Studded(),
        Category.Female      => Female(),
        _                    => null
    };

    private static Type[] GetTypes(Category category) => category switch
    {
        Category.Shirts      => ShirtsTypes,
        Category.Pants       => PantsTypes,
        Category.Misc        => MiscTypes,
        Category.Footwear    => FootwearTypes,
        Category.Leather     => LeatherArmorTypes,
        Category.Studded     => StuddedArmorTypes,
        Category.Female      => FemaleArmorTypes,
        _                    => null
    };

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("Build Shirts", 0x1517, 0, (int)Category.Shirts),
        new ItemListEntry("Build Pants", 0x1539, 0, (int)Category.Pants),
        new ItemListEntry("Build Misc", 0x153D, 0, (int)Category.Misc)
    ];

    public static ItemListEntry[] LeatherMain() => _leatherMainEntries ??=
    [
        new ItemListEntry("Build Shoes", 0x170f, 0, (int)Category.Footwear),
        new ItemListEntry("Build Leather Armor", 0x13cc, 0, (int)Category.Leather),
        new ItemListEntry("Build Studded Armor", 0x13db, 0, (int)Category.Studded),
        new ItemListEntry("Build Female Armor", 0x1c06, 0, (int)Category.Female)
    ];

    public static ItemListEntry[] Shirts() => _shirtsEntries ??= BuildStaticEntries(ShirtsTypes, "cloth");
    public static ItemListEntry[] Pants() => _pantsEntries ??= BuildStaticEntries(PantsTypes, "cloth");
    public static ItemListEntry[] Misc() => _miscEntries ??= BuildStaticEntries(MiscTypes, "cloth");
    public static ItemListEntry[] Footwear() => _footwearEntries ??= BuildStaticEntries(FootwearTypes, "leather");
    public static ItemListEntry[] Leather() => _leatherEntries ??= BuildStaticEntries(LeatherArmorTypes, "leather");
    public static ItemListEntry[] Studded() => _studdedEntries ??= BuildStaticEntries(StuddedArmorTypes, "leather");
    public static ItemListEntry[] Female() => _femaleEntries ??= BuildStaticEntries(FemaleArmorTypes, "leather");

    private static ItemListEntry[] BuildFilteredEntries(Mobile from, Category category)
    {
        if (category is Category.Main or Category.LeatherMain)
        {
            return BuildFilteredMainEntries(from, category);
        }

        var types = GetTypes(category);
        var staticEntries = GetStaticEntries(category);
        if (types == null || staticEntries == null)
        {
            return [];
        }

        return T2ACraftSystem.FilterEntries(from, staticEntries, types, DefTailoring.CraftSystem);
    }

    private static ItemListEntry[] BuildFilteredMainEntries(Mobile from, Category mainCategory)
    {
        var system = DefTailoring.CraftSystem;
        var mainStatic = GetStaticEntries(mainCategory);
        if (mainStatic == null)
        {
            return [];
        }

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

    private void CraftItem(Mobile from, Type itemType)
    {
        var itemDef = DefTailoring.CraftSystem.CraftItems.SearchFor(itemType);
        if (itemDef == null)
        {
            return;
        }

        var num = DefTailoring.CraftSystem.CanCraft(from, _tool, itemDef.ItemType);
        if (num > 0)
        {
            from.SendLocalizedMessage(num);
            return;
        }

        var context = DefTailoring.CraftSystem.GetContext(from);
        var res = itemDef.UseSubRes2 ? DefTailoring.CraftSystem.CraftSubRes2 : DefTailoring.CraftSystem.CraftSubRes;
        var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        var resourceType = resIndex > -1 ? res[resIndex].ItemType : null;

        if (_hue >= 0)
        {
            // Pipeline B: hue-aware crafting — only consumes resources matching this hue
            DefTailoring.CraftSystem.CreateItem(from, itemDef.ItemType, resourceType, _tool, itemDef, _hue);
        }
        else
        {
            DefTailoring.CraftSystem.CreateItem(from, itemDef.ItemType, resourceType, _tool, itemDef);
        }
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = Entries[index].CraftIndex;

        if (_category is Category.Main or Category.LeatherMain)
        {
            // Carry hue through to child menus
            var menu = new TailoringMenu(from, _tool, (Category)craftIndex, _hue);
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
            CraftItem(from, types[craftIndex]);
        }
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
            from.SendAsciiMessage("Select the resource you wish to use (cloth, leather, or hides).");
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Cloth or UncutCloth)
            {
                var hue = ((Item)targeted).Hue;
                var menu = new TailoringMenu(from, _tool, Category.Main, hue);
                if (menu.Entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                    return;
                }

                from.SendMenu(menu);
            }
            else if (targeted is Items.Leather or Hides)
            {
                var hue = ((Item)targeted).Hue;
                var menu = new TailoringMenu(from, _tool, Category.LeatherMain, hue);
                if (menu.Entries.Length == 0)
                {
                    from.SendAsciiMessage("You lack the skill and materials to craft anything.");
                    return;
                }

                from.SendMenu(menu);
            }
            else
            {
                from.SendAsciiMessage("That is not a valid resource. Please select cloth, leather, or hides.");
                from.Target = new ResourceSelectTarget(_from, _tool);
            }
        }
    }
}
