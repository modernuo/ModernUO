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
        Metal
    }

    private static readonly Type[] WoodItemTypes =
    [
        typeof(JointingPlane), typeof(MouldingPlane), typeof(SmoothingPlane),
        typeof(ClockFrame), typeof(Axle), typeof(RollingPin)
    ];

    private static readonly int[] WoodItemIDs = [0x1030, 0x102C, 0x1032, 0x104D, 0x105B, 0x1043];

    private static readonly Type[] MetalItemTypes =
    [
        typeof(DartTrapCraft), typeof(ExplosionTrapCraft), typeof(PoisonTrapCraft),
        typeof(SewingKit), typeof(Gears), typeof(Springs), typeof(Hinge),
        typeof(DrawKnife), typeof(Froe), typeof(Inshave), typeof(Scorp),
        typeof(ButcherKnife), typeof(Scissors), typeof(Tongs),
        typeof(DovetailSaw), typeof(Saw), typeof(Hammer),
        typeof(SmithHammer), typeof(SledgeHammer), typeof(Shovel)
    ];

    private static readonly int[] MetalItemIDs =
    [
        4397, 4344, 4424,
        0xF9D, 0x1053, 0x105D, 0x1055,
        0x10E4, 0x10E5, 0x10E6, 0x10E7,
        0x13F6, 0xF9F, 0xFB5,
        0x1028, 0x1034, 0x102A,
        0x13E3, 0xFB5, 0xF39
    ];

    private static ItemListEntry[] _mainEntries;
    private static ItemListEntry[] _woodEntries;
    private static ItemListEntry[] _metalEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;
    private readonly Type _selectedResourceType;

    public TinkeringMenu(BaseTool tool) : this(tool, Category.Main, null)
    {
    }

    private TinkeringMenu(BaseTool tool, Category category, Type selectedResourceType)
        : base("Choose an item.", GetEntries(category))
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

    private static ItemListEntry[] BuildStaticEntries(Type[] types, int[] itemIDs)
    {
        var entries = new ItemListEntry[types.Length];
        var count = 0;
        var craftItems = DefTinkering.CraftSystem.CraftItems;

        for (var i = 0; i < types.Length; i++)
        {
            if (craftItems.SearchFor(types[i]) == null)
            {
                continue;
            }

            var name = FormatItemName(types[i]);
            entries[count++] = new ItemListEntry(name, itemIDs[i], 0, i);
        }

        if (count < entries.Length)
        {
            Array.Resize(ref entries, count);
        }

        return entries;
    }

    private static ItemListEntry[] GetEntries(Category category) => category switch
    {
        Category.Main  => Main(),
        Category.Wood  => Wood(),
        Category.Metal => Metal(),
        _              => null
    };

    private static Type[] GetTypes(Category category) => category switch
    {
        Category.Wood  => WoodItemTypes,
        Category.Metal => MetalItemTypes,
        _              => null
    };

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("Wooden Items", 0x1BDD, 0, (int)Category.Wood),
        new ItemListEntry("Metal Items", 0x1BF2, 0, (int)Category.Metal)
    ];

    public static ItemListEntry[] Wood() => _woodEntries ??= BuildStaticEntries(WoodItemTypes, WoodItemIDs);
    public static ItemListEntry[] Metal() => _metalEntries ??= BuildStaticEntries(MetalItemTypes, MetalItemIDs);

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = GetEntries(_category)[index].CraftIndex;

        if (_category == Category.Main)
        {
            var childCategory = (Category)craftIndex;
            var resourceType = childCategory == Category.Wood ? typeof(Log) : typeof(IronIngot);
            from.SendMenu(new TinkeringMenu(_tool, childCategory, resourceType));
            return;
        }

        var types = GetTypes(_category);
        if (types == null || craftIndex < 0 || craftIndex >= types.Length)
        {
            return;
        }

        var itemType = types[craftIndex];
        var itemDef = DefTinkering.CraftSystem.CraftItems.SearchFor(itemType);
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
                from.SendMenu(new TinkeringMenu(_tool, Category.Wood, typeof(Log)));
            }
            else if (targeted is IronIngot)
            {
                from.SendMenu(new TinkeringMenu(_tool, Category.Metal, typeof(IronIngot)));
            }
            else
            {
                from.SendAsciiMessage("That is not a valid resource. Please select log or iron ingot.");
                from.Target = new ResourceSelectTarget(_from, _tool);
            }
        }
    }
}
