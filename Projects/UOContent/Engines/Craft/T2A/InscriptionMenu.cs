using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.Engines.Craft.T2A;

public class InscriptionMenu : ItemListMenu
{
    private enum Category
    {
        Main,
        Circle1,
        Circle2,
        Circle3,
        Circle4,
        Circle5,
        Circle6,
        Circle7,
        Circle8
    }

    private static readonly ItemListEntry[][] _circleEntries = new ItemListEntry[8][];

    private static ItemListEntry[] _mainEntries;

    private readonly Category _category;
    private readonly BaseTool _tool;

    public InscriptionMenu(Mobile from, BaseTool tool) : this(from, tool, Category.Main)
    {
    }

    private static string GetQuestion(Category category) => category switch
    {
        Category.Main => "Which circle of spells?",
        _             => "Which spell would you like to scribe?"
    };

    private InscriptionMenu(Mobile from, BaseTool tool, Category category)
        : base(GetQuestion(category), BuildFilteredEntries(from, category))
    {
        _tool = tool;
        _category = category;
    }

    private static string FormatScrollName(Type type)
    {
        var name = type.Name;
        if (name.EndsWith("Scroll"))
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

    public static ItemListEntry[] Main() => _mainEntries ??=
    [
        new ItemListEntry("First Circle", 8384, 0, (int)Category.Circle1),
        new ItemListEntry("Second Circle", 8385, 0, (int)Category.Circle2),
        new ItemListEntry("Third Circle", 8386, 0, (int)Category.Circle3),
        new ItemListEntry("Fourth Circle", 8387, 0, (int)Category.Circle4),
        new ItemListEntry("Fifth Circle", 8388, 0, (int)Category.Circle5),
        new ItemListEntry("Sixth Circle", 8389, 0, (int)Category.Circle6),
        new ItemListEntry("Seventh Circle", 8390, 0, (int)Category.Circle7),
        new ItemListEntry("Eighth Circle", 8391, 0, (int)Category.Circle8)
    ];

    private static ItemListEntry[] BuildCircleEntries(int circleIndex)
    {
        var offset = circleIndex * 8;
        var craftItems = DefInscription.CraftSystem.CraftItems;
        var entries = new ItemListEntry[8];
        var count = 0;

        for (var i = 0; i < 8; i++)
        {
            var itemDef = craftItems[offset + i];
            var name = FormatScrollName(itemDef.ItemType);
            entries[count++] = new ItemListEntry(name, 8320 + offset + i, 0, i);
        }

        if (count < entries.Length)
        {
            Array.Resize(ref entries, count);
        }

        return entries;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ItemListEntry[] GetCircleEntries(int circleIndex) =>
        _circleEntries[circleIndex] ??= BuildCircleEntries(circleIndex);

    private static Dictionary<Type, int> _spellIds;
    private static object[] _args;

    private static bool HasSpellInBook(Mobile from, Type scrollType)
    {
        if (scrollType == null)
        {
            return false;
        }

        _spellIds ??= [];
        if (!_spellIds.TryGetValue(scrollType, out var spellId))
        {
            try
            {
                _args ??= [1];
                var scroll = scrollType.CreateInstance<SpellScroll>(_args);
                if (scroll != null)
                {
                    spellId = _spellIds[scrollType] = scroll.SpellID;
                    scroll.Delete();
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        var book = Spellbook.Find(from, spellId);
        return book?.HasSpell(spellId) ?? false;
    }

    private static bool CanScribeScroll(Mobile from, CraftItem itemDef)
    {
        // Skill check
        if (!T2ACraftSystem.CanCraftItem(from, itemDef, DefInscription.CraftSystem))
        {
            return false;
        }

        // Must have blank scrolls
        if ((from.Backpack?.GetAmount(typeof(BlankScroll)) ?? 0) == 0)
        {
            return false;
        }

        // Must have the spell in spellbook
        return HasSpellInBook(from, itemDef.ItemType);
    }

    private static ItemListEntry[] BuildFilteredEntries(Mobile from, Category category)
    {
        if (category == Category.Main)
        {
            return BuildFilteredMainEntries(from);
        }

        var circleIndex = (int)category - 1;
        var staticEntries = GetCircleEntries(circleIndex);
        var craftItems = DefInscription.CraftSystem.CraftItems;
        var offset = circleIndex * 8;

        var filtered = new ItemListEntry[staticEntries.Length];
        var count = 0;

        for (var i = 0; i < staticEntries.Length; i++)
        {
            var entry = staticEntries[i];
            var scrollIndex = entry.CraftIndex;
            var itemDef = craftItems[offset + scrollIndex];
            if (CanScribeScroll(from, itemDef))
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
        var craftItems = DefInscription.CraftSystem.CraftItems;
        var mainStatic = Main();
        var filtered = new ItemListEntry[mainStatic.Length];
        var count = 0;

        for (var i = 0; i < mainStatic.Length; i++)
        {
            var entry = mainStatic[i];
            var circleIndex = (int)(Category)entry.CraftIndex - 1;
            var offset = circleIndex * 8;

            var hasAny = false;
            for (var j = 0; j < 8; j++)
            {
                var itemDef = craftItems[offset + j];
                if (CanScribeScroll(from, itemDef))
                {
                    hasAny = true;
                    break;
                }
            }

            if (hasAny)
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

    private void CraftScroll(Mobile from, Category circle, int scrollIndex)
    {
        var itemIndex = ((int)circle - 1) * 8 + scrollIndex;
        var craftItems = DefInscription.CraftSystem.CraftItems;
        var itemDef = craftItems[itemIndex];

        if (!HasSpellInBook(from, itemDef.ItemType))
        {
            from.SendAsciiMessage("You do not have that spell in your spellbook.");
            return;
        }

        var context = DefInscription.CraftSystem.GetContext(from);
        var res = itemDef.UseSubRes2 ? DefInscription.CraftSystem.CraftSubRes2 : DefInscription.CraftSystem.CraftSubRes;
        var resIndex = itemDef.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
        var type = resIndex > -1 ? res[resIndex].ItemType : null;
        DefInscription.CraftSystem.CreateItem(from, itemDef.ItemType, type, _tool, itemDef);
    }

    public override void OnResponse(NetState state, int index)
    {
        var from = state.Mobile;

        if ((from.Backpack?.GetAmount(typeof(BlankScroll)) ?? 0) == 0)
        {
            from.SendAsciiMessage("You do not have enough blank scrolls to make that.");
            return;
        }

        var craftIndex = Entries[index].CraftIndex;
        if (_category == Category.Main)
        {
            var menu = new InscriptionMenu(from, _tool, (Category)craftIndex);
            if (menu.Entries.Length == 0)
            {
                from.SendAsciiMessage("You lack the skill and materials to scribe anything in that circle.");
                return;
            }

            from.SendMenu(menu);
            return;
        }

        CraftScroll(from, _category, craftIndex);
    }
}
