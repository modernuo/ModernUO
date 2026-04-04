using System;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.Engines.Craft.T2A;

public class AlchemyMenu : ItemListMenu
{
    private enum Category
    {
        Main,
        Refresh,
        Agility,
        NightSight,
        Heal,
        Strength,
        Poison,
        Cure,
        Explosion
    }

    private static readonly Type[] RefreshTypes = [typeof(RefreshPotion), typeof(TotalRefreshPotion)];

    private static readonly Type[] AgilityTypes = [typeof(AgilityPotion), typeof(GreaterAgilityPotion)];

    private static readonly Type[] NightSightTypes = [typeof(NightSightPotion)];

    private static readonly Type[] HealTypes = [typeof(LesserHealPotion), typeof(HealPotion), typeof(GreaterHealPotion)];

    private static readonly Type[] StrengthTypes = [typeof(StrengthPotion), typeof(GreaterStrengthPotion)];

    private static readonly Type[] PoisonTypes =
    [
        typeof(LesserPoisonPotion), typeof(PoisonPotion), typeof(GreaterPoisonPotion), typeof(DeadlyPoisonPotion)
    ];

    private static readonly Type[] CureTypes = [typeof(LesserCurePotion), typeof(CurePotion), typeof(GreaterCurePotion)];

    private static readonly Type[] ExplosionTypes =
    [
        typeof(LesserExplosionPotion), typeof(ExplosionPotion), typeof(GreaterExplosionPotion)
    ];

    private static ItemListEntry[] _mainEntries;
    private static ItemListEntry[] _refreshEntries;
    private static ItemListEntry[] _agilityEntries;
    private static ItemListEntry[] _nightSightEntries;
    private static ItemListEntry[] _healEntries;
    private static ItemListEntry[] _strengthEntries;
    private static ItemListEntry[] _poisonEntries;
    private static ItemListEntry[] _cureEntries;
    private static ItemListEntry[] _explosionEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;

    public AlchemyMenu(Mobile from, BaseTool tool) : this(from, tool, Category.Main)
    {
    }

    private static string GetQuestion(Category category) => category switch
    {
        Category.Main => "What kind of potion?",
        _             => "Which potion would you like to make?"
    };

    private AlchemyMenu(Mobile from, BaseTool tool, Category category)
        : base(GetQuestion(category), BuildFilteredEntries(from, category))
    {
        _tool = tool;
        _category = category;
    }

    private static string FormatItemName(Type type)
    {
        var name = type.Name;

        if (name.EndsWith("Potion"))
        {
            name = name[..^6];
        }

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

    private static ItemListEntry[] BuildStaticEntries(Type[] types)
    {
        var entries = new ItemListEntry[types.Length];
        var count = 0;
        var craftItems = DefAlchemy.CraftSystem.CraftItems;

        for (var i = 0; i < types.Length; i++)
        {
            var itemDef = craftItems.SearchFor(types[i]);
            if (itemDef == null)
            {
                continue;
            }

            entries[count++] = new ItemListEntry(FormatItemName(types[i]), itemDef.ItemId, 0, i);
        }

        if (count < entries.Length)
        {
            Array.Resize(ref entries, count);
        }

        return entries;
    }

    private static ItemListEntry[] GetStaticEntries(Category category) => category switch
    {
        Category.Main       => Main(),
        Category.Refresh    => _refreshEntries ??= BuildStaticEntries(RefreshTypes),
        Category.Agility    => _agilityEntries ??= BuildStaticEntries(AgilityTypes),
        Category.NightSight => _nightSightEntries ??= BuildStaticEntries(NightSightTypes),
        Category.Heal       => _healEntries ??= BuildStaticEntries(HealTypes),
        Category.Strength   => _strengthEntries ??= BuildStaticEntries(StrengthTypes),
        Category.Poison     => _poisonEntries ??= BuildStaticEntries(PoisonTypes),
        Category.Cure       => _cureEntries ??= BuildStaticEntries(CureTypes),
        Category.Explosion  => _explosionEntries ??= BuildStaticEntries(ExplosionTypes),
        _                   => null
    };

    private static Type[] GetTypes(Category category) => category switch
    {
        Category.Refresh    => RefreshTypes,
        Category.Agility    => AgilityTypes,
        Category.NightSight => NightSightTypes,
        Category.Heal       => HealTypes,
        Category.Strength   => StrengthTypes,
        Category.Poison     => PoisonTypes,
        Category.Cure       => CureTypes,
        Category.Explosion  => ExplosionTypes,
        _                   => null
    };

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("Refresh", 0xF0B, 0, (int)Category.Refresh),
        new ItemListEntry("Agility", 0xF08, 0, (int)Category.Agility),
        new ItemListEntry("Night Sight", 0xF06, 0, (int)Category.NightSight),
        new ItemListEntry("Heal", 0xF0C, 0, (int)Category.Heal),
        new ItemListEntry("Strength", 0xF09, 0, (int)Category.Strength),
        new ItemListEntry("Poison", 0xF0A, 0, (int)Category.Poison),
        new ItemListEntry("Cure", 0xF07, 0, (int)Category.Cure),
        new ItemListEntry("Explosion", 0xF0D, 0, (int)Category.Explosion)
    ];

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

        return T2ACraftSystem.FilterEntries(from, staticEntries, types, DefAlchemy.CraftSystem);
    }

    private static ItemListEntry[] BuildFilteredMainEntries(Mobile from)
    {
        var system = DefAlchemy.CraftSystem;
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

    private void CraftPotion(Mobile from, Type potionType)
    {
        if ((from.Backpack?.GetAmount(typeof(Bottle)) ?? 0) == 0)
        {
            from.SendAsciiMessage("You need an empty bottle to make a potion.");
            return;
        }

        var itemDef = DefAlchemy.CraftSystem.CraftItems.SearchFor(potionType);

        if (itemDef == null)
        {
            return;
        }

        var num = DefAlchemy.CraftSystem.CanCraft(from, _tool, itemDef.ItemType);

        if (num > 0)
        {
            from.SendLocalizedMessage(num);
            return;
        }

        var res = itemDef.Resources[0];
        DefAlchemy.CraftSystem.CreateItem(from, itemDef.ItemType, res.ItemType, _tool, itemDef);
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;
        var craftIndex = Entries[index].CraftIndex;

        if (_category == Category.Main)
        {
            var menu = new AlchemyMenu(from, _tool, (Category)craftIndex);
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
            CraftPotion(from, types[craftIndex]);
        }
    }
}
