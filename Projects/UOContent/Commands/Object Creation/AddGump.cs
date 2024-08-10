using System;
using System.Collections.Generic;
using Server.Network;
using Server.Targeting;

namespace Server.Gumps;

public class AddGump : DynamicGump
{
    private static readonly Type _typeofItem = typeof(Item);
    private static readonly Type _typeofMobile = typeof(Mobile);
    private readonly int _page;
    private readonly Type[] _searchResults;
    private readonly string _searchString;
    private readonly bool _explicitSearch;

    public override bool Singleton => true;

    public AddGump(string searchString, int page, Type[] searchResults, bool explicitSearch) : base(50, 50)
    {
        _searchString = searchString;
        _searchResults = searchResults;
        _explicitSearch = explicitSearch;
        _page = page;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder) {

        builder.AddPage();

        builder.AddBackground(0, 0, 420, 280, 5054);

        builder.AddImageTiled(10, 10, 400, 20, 2624);
        builder.AddAlphaRegion(10, 10, 400, 20);
        builder.AddImageTiled(41, 11, 184, 18, 0xBBC);
        builder.AddImageTiled(42, 12, 182, 16, 2624);
        builder.AddAlphaRegion(42, 12, 182, 16);

        builder.AddButton(10, 9, 4011, 4013, 1);
        builder.AddTextEntry(44, 10, 180, 20, 0x480, 0, _searchString);

        builder.AddHtmlLocalized(230, 10, 100, 20, 3010005, 0x7FFF);

        builder.AddImageTiled(10, 40, 400, 200, 2624);
        builder.AddAlphaRegion(10, 40, 400, 200);

        if (_searchResults.Length > 0)
        {
            for (var i = _page * 10; i < (_page + 1) * 10 && i < _searchResults.Length; ++i)
            {
                var index = i % 10;

                builder.AddLabel(44, 39 + index * 20, 0x480, _searchResults[i].Name);
                builder.AddButton(10, 39 + index * 20, 4023, 4025, 4 + i);
            }
        }
        else
        {
            builder.AddLabel(15, 44, 0x480, _explicitSearch ? "Nothing matched your search terms." : "No results to display.");
        }

        builder.AddImageTiled(10, 250, 400, 20, 2624);
        builder.AddAlphaRegion(10, 250, 400, 20);

        if (_page > 0)
        {
            builder.AddButton(10, 249, 4014, 4016, 2);
        }
        else
        {
            builder.AddImage(10, 249, 4014);
        }

        builder.AddHtmlLocalized(44, 250, 170, 20, 1061028, _page > 0 ? 0x7FFF : 0x5EF7); // Previous page

        if ((_page + 1) * 10 < _searchResults.Length)
        {
            builder.AddButton(210, 249, 4005, 4007, 3);
        }
        else
        {
            builder.AddImage(210, 249, 4005);
        }

        builder.AddHtmlLocalized(
            244,
            250,
            170,
            20,
            1061027, // Next page
            (_page + 1) * 10 < _searchResults.Length ? 0x7FFF : 0x5EF7
        );
    }

    public static void Configure()
    {
        CommandSystem.Register("AddMenu", AccessLevel.GameMaster, AddMenu_OnCommand);
    }

    [Usage("AddMenu [searchString]")]
    [Description("Opens an add menu, with an optional initial search string. This menu allows you to search for Items or Mobiles and add them interactively.")]
    private static void AddMenu_OnCommand(CommandEventArgs e)
    {
        var val = e.ArgString.Trim();
        Type[] types;
        var explicitSearch = false;

        if (val.Length == 0)
        {
            types = Type.EmptyTypes;
        }
        else if (val.Length < 3)
        {
            e.Mobile.SendMessage("Invalid search string.");
            types = Type.EmptyTypes;
        }
        else
        {
            types = Match(val);
            explicitSearch = true;
        }

        e.Mobile.SendGump(new AddGump(val, 0, types, explicitSearch));
    }

    private static void Match(string match, Type[] types, HashSet<Type> results)
    {
        if (match.Length == 0)
        {
            return;
        }

        match = match.ToLower();

        for (var i = 0; i < types.Length; i++)
        {
            var t = types[i];

            if (!(_typeofMobile.IsAssignableFrom(t) || _typeofItem.IsAssignableFrom(t)) ||
                !t.Name.InsensitiveContains(match) || results.Contains(t))
            {
                continue;
            }

            var ctors = t.GetConstructors();

            for (var j = 0; j < ctors.Length; j++)
            {
                var ctor = ctors[j];
                var isEmptyCtor = true;
                var paramsList = ctor.GetParameters();
                for (var k = 0; k < paramsList.Length; k++)
                {
                    if (!paramsList[k].HasDefaultValue)
                    {
                        isEmptyCtor = false;
                        break;
                    }
                }

                if (isEmptyCtor && ctors[j].IsDefined(typeof(ConstructibleAttribute), false))
                {
                    results.Add(t);
                    break;
                }
            }
        }
    }

    public static Type[] Match(string match)
    {
        var results = new HashSet<Type>();
        Type[] types;

        var asms = AssemblyHandler.Assemblies;

        for (var i = 0; i < asms.Length; ++i)
        {
            types = AssemblyHandler.GetTypeCache(asms[i]).Types;
            Match(match, types, results);
        }

        types = AssemblyHandler.GetTypeCache(Core.Assembly).Types;
        Match(match, types, results);

        if (results.Count == 0)
        {
            return Array.Empty<Type>();
        }

        var finalResults = new Type[results.Count];
        var index = 0;
        foreach (var t in results)
        {
            finalResults[index++] = t;
        }

        Array.Sort(finalResults, TypeNameComparer.Instance);
        return finalResults;
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        switch (info.ButtonID)
        {
            case 1: // Search
                {
                    var match = info.GetTextEntry(0)?.Trim() ?? "";

                    if (match.Length < 3)
                    {
                        from.SendMessage("Invalid search string.");
                        from.SendGump(new AddGump(match, _page, _searchResults, false));
                    }
                    else
                    {
                        from.SendGump(new AddGump(match, 0, Match(match), true));
                    }

                    break;
                }
            case 2: // Previous page
                {
                    if (_page > 0)
                    {
                        from.SendGump(new AddGump(_searchString, _page - 1, _searchResults, true));
                    }

                    break;
                }
            case 3: // Next page
                {
                    if ((_page + 1) * 10 < _searchResults.Length)
                    {
                        from.SendGump(new AddGump(_searchString, _page + 1, _searchResults, true));
                    }

                    break;
                }
            default:
                {
                    var index = info.ButtonID - 4;

                    if (index >= 0 && index < _searchResults.Length)
                    {
                        from.SendMessage("Where do you wish to place this object? <ESC> to cancel.");
                        from.Target = new InternalTarget(
                            _searchResults[index],
                            _searchResults,
                            _searchString,
                            _page
                        );
                    }

                    break;
                }
        }
    }

    private class TypeNameComparer : IComparer<Type>
    {
        public static readonly TypeNameComparer Instance = new();
        public int Compare(Type x, Type y) => string.CompareOrdinal(x?.Name, y?.Name);
    }

    public class InternalTarget : Target
    {
        private readonly int m_Page;
        private readonly Type[] m_SearchResults;
        private readonly string m_SearchString;
        private readonly Type m_Type;

        public InternalTarget(Type type, Type[] searchResults, string searchString, int page) : base(
            -1,
            true,
            TargetFlags.None
        )
        {
            m_Type = type;
            m_SearchResults = searchResults;
            m_SearchString = searchString;
            m_Page = page;
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is not IPoint3D ip)
            {
                return;
            }

            Point3D p = ip switch
            {
                Item item => item.GetWorldTop(),
                Mobile m  => m.Location,
                _         => new Point3D(ip)
            };

            Commands.Add.Invoke(from, new Point3D(p), new Point3D(p), new[] { m_Type.Name });

            from.Target = new InternalTarget(m_Type, m_SearchResults, m_SearchString, m_Page);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            if (cancelType == TargetCancelType.Canceled)
            {
                from.SendGump(new AddGump(m_SearchString, m_Page, m_SearchResults, true));
            }
        }
    }
}
