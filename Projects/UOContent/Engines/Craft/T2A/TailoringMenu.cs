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
        BoltOfCloth,
        Footwear,
        Leather,
        Studded,
        Female
    }

    private static readonly Type[] ShirtsTypes =
    [
        typeof(Doublet), typeof(Shirt), typeof(FancyShirt), typeof(Tunic), typeof(Surcoat), typeof(PlainDress)
    ];

    private static readonly Type[] PantsTypes =
    [
        typeof(ShortPants), typeof(LongPants), typeof(Kilt)
    ];

    private static readonly Type[] MiscTypes =
    [
        typeof(Skirt), typeof(Cloak), typeof(Robe), typeof(JesterSuit), typeof(FancyDress)
    ];

    private static readonly Type[] BoltTypes =
    [
        typeof(BoltOfCloth)
    ];

    private static readonly Type[] FootwearTypes =
    [
        typeof(Sandals), typeof(Shoes), typeof(Boots), typeof(ThighBoots)
    ];

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
    private static ItemListEntry[] _boltEntries;
    private static ItemListEntry[] _footwearEntries;
    private static ItemListEntry[] _leatherEntries;
    private static ItemListEntry[] _studdedEntries;
    private static ItemListEntry[] _femaleEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;

    public TailoringMenu(BaseTool tool) : this(tool, Category.Main)
    {
    }

    private TailoringMenu(BaseTool tool, Category category)
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

    private static ItemListEntry[] GetEntries(Category category) => category switch
    {
        Category.Main        => Main(),
        Category.LeatherMain => LeatherMain(),
        Category.Shirts      => Shirts(),
        Category.Pants       => Pants(),
        Category.Misc        => Misc(),
        Category.BoltOfCloth => Bolt(),
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
        Category.BoltOfCloth => BoltTypes,
        Category.Footwear    => FootwearTypes,
        Category.Leather     => LeatherArmorTypes,
        Category.Studded     => StuddedArmorTypes,
        Category.Female      => FemaleArmorTypes,
        _                    => null
    };

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("Shirts", 0x1517, 0, (int)Category.Shirts),
        new ItemListEntry("Pants", 0x1539, 0, (int)Category.Pants),
        new ItemListEntry("Miscellaneous", 0x153D, 0, (int)Category.Misc),
        new ItemListEntry("Bolt of Cloth", 0x0F95, 0, (int)Category.BoltOfCloth)
    ];

    public static ItemListEntry[] LeatherMain() => _leatherMainEntries ??=
    [
        new ItemListEntry("Footwear", 0x170f, 0, (int)Category.Footwear),
        new ItemListEntry("Leather Armor", 0x13cc, 0, (int)Category.Leather),
        new ItemListEntry("Studded Armor", 0x13db, 0, (int)Category.Studded),
        new ItemListEntry("Female Armor", 0x1c06, 0, (int)Category.Female)
    ];

    public static ItemListEntry[] Shirts() => _shirtsEntries ??= BuildStaticEntries(ShirtsTypes, "cloth");
    public static ItemListEntry[] Pants() => _pantsEntries ??= BuildStaticEntries(PantsTypes, "cloth");
    public static ItemListEntry[] Misc() => _miscEntries ??= BuildStaticEntries(MiscTypes, "cloth");
    public static ItemListEntry[] Bolt() => _boltEntries ??= BuildStaticEntries(BoltTypes, "cloth");
    public static ItemListEntry[] Footwear() => _footwearEntries ??= BuildStaticEntries(FootwearTypes, "leather");
    public static ItemListEntry[] Leather() => _leatherEntries ??= BuildStaticEntries(LeatherArmorTypes, "leather");
    public static ItemListEntry[] Studded() => _studdedEntries ??= BuildStaticEntries(StuddedArmorTypes, "leather");
    public static ItemListEntry[] Female() => _femaleEntries ??= BuildStaticEntries(FemaleArmorTypes, "leather");

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
        DefTailoring.CraftSystem.CreateItem(from, itemDef.ItemType, resourceType, _tool, itemDef);
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = GetEntries(_category)[index].CraftIndex;

        if (_category == Category.Main || _category == Category.LeatherMain)
        {
            from.SendMenu(new TailoringMenu(_tool, (Category)craftIndex));
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
            if (targeted is Cloth)
            {
                from.SendMenu(new TailoringMenu(_tool));
            }
            else if (targeted is Items.Leather or Hides)
            {
                from.SendMenu(new TailoringMenu(_tool, Category.LeatherMain));
            }
            else
            {
                from.SendAsciiMessage("That is not a valid resource. Please select cloth, leather, or hides.");
                from.Target = new ResourceSelectTarget(_from, _tool);
            }
        }
    }
}
