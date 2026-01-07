using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Server.Network;
using Server.Targeting;

namespace Server.Gumps;

public class AddGump : DynamicGump
{
    private static readonly Type _typeofItem = typeof(Item);
    private static readonly Type _typeofMobile = typeof(Mobile);
    private readonly int _page;
    private readonly ConstructorInfo[] _searchResults;
    private readonly string _searchString;
    private readonly bool _explicitSearch;

    public override bool Singleton => true;

    public AddGump(string searchString, int page, ConstructorInfo[] searchResults, bool explicitSearch) : base(50, 50)
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

                builder.AddLabel(44, 39 + index * 20, 0x480, _searchResults[i].DeclaringType!.Name);
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
        ConstructorInfo[] ctors;
        var explicitSearch = false;

        if (val.Length == 0)
        {
            ctors = [];
        }
        else if (val.Length < 3)
        {
            e.Mobile.SendMessage("Invalid search string.");
            ctors = [];
        }
        else
        {
            ctors = MatchEmptyCtor(val);
            explicitSearch = true;
        }

        e.Mobile.SendGump(new AddGump(val, 0, ctors, explicitSearch));
    }

    private static bool ExactMatch(string match, Assembly assembly, out Type type)
    {
        if (!_mobileItemTypes.TryGetValue(assembly, out var mobileItemTypes))
        {
            List<Type> typeList = [];
            var types = AssemblyHandler.GetTypeCache(assembly).Types;
            for (var i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (_typeofMobile.IsAssignableFrom(t) || _typeofItem.IsAssignableFrom(t))
                {
                    typeList.Add(t);
                }
            }

            _mobileItemTypes[assembly] = mobileItemTypes = typeList.ToArray();
        }

        for (var i = 0; i < mobileItemTypes.Length; i++)
        {
            var t = mobileItemTypes[i];

            if (t.Name.InsensitiveEquals(match))
            {
                type = t;
                return true;
            }
        }

        type = null;
        return false;
    }

    private static void MatchEmptyCtor(string match, Assembly assembly, List<ConstructorInfo> results)
    {
        if (!_mobileItemTypes.TryGetValue(assembly, out var mobileItemTypes))
        {
            List<Type> typeList = [];
            var types = AssemblyHandler.GetTypeCache(assembly).Types;
            for (var i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (_typeofMobile.IsAssignableFrom(t) || _typeofItem.IsAssignableFrom(t))
                {
                    typeList.Add(t);
                }
            }

            _mobileItemTypes[assembly] = mobileItemTypes = typeList.ToArray();
        }

        for (var i = 0; i < mobileItemTypes.Length; i++)
        {
            var t = mobileItemTypes[i];

            if (!t.Name.InsensitiveContains(match))
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

                if (isEmptyCtor && ctor.GetCustomAttributes(Types.OfConstructible, false).Length > 0)
                {
                    results.Add(ctor);
                    break;
                }
            }
        }
    }

    private static readonly Dictionary<Assembly, Type[]> _mobileItemTypes = [];

    public static ConstructorInfo[] MatchEmptyCtor(string match)
    {
        if (string.IsNullOrWhiteSpace(match))
        {
            return [];
        }

        match = match.ToLower();

        List<ConstructorInfo> results = [];
        MatchEmptyCtor(match, Core.Assembly, results);

        for (var i = 0; i < AssemblyHandler.Assemblies.Length; ++i)
        {
            MatchEmptyCtor(match, AssemblyHandler.Assemblies[i], results);
        }

        if (results.Count == 0)
        {
            return [];
        }

        var finalResults = results.ToArray();
        Array.Sort(finalResults, ConstructorNameComparer.Instance);

        return finalResults;
    }

    public static Type ExactMatch(string match)
    {
        if (string.IsNullOrWhiteSpace(match))
        {
            return null;
        }

        match = match.ToLower();

        if (ExactMatch(match, Core.Assembly, out var type))
        {
            return type;
        }

        for (var i = 0; i < AssemblyHandler.Assemblies.Length; ++i)
        {
            if (ExactMatch(match, AssemblyHandler.Assemblies[i], out type))
            {
                return type;
            }
        }

        return null;
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
                        from.SendGump(new AddGump(match, 0, MatchEmptyCtor(match), true));
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

    private class ConstructorNameComparer : IComparer<ConstructorInfo>
    {
        public static readonly ConstructorNameComparer Instance = new();
        public int Compare(ConstructorInfo x, ConstructorInfo y) =>
            x?.DeclaringType!.Name.CompareOrdinal(y?.DeclaringType!.Name) ?? 0;
    }

    public class InternalTarget : Target
    {
        private readonly int _page;
        private readonly ConstructorInfo[] _searchResults;
        private readonly string _searchString;
        private readonly ConstructorInfo _ctor;

        public InternalTarget(ConstructorInfo ctor, ConstructorInfo[] searchResults, string searchString, int page) : base(
            -1,
            true,
            TargetFlags.None
        )
        {
            _ctor = ctor;
            _searchResults = searchResults;
            _searchString = searchString;
            _page = page;
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is not IPoint3D ip)
            {
                return;
            }

            var p = ip switch
            {
                Item item => item.GetWorldTop(),
                Mobile m  => m.Location,
                _         => new Point3D(ip)
            };

            Commands.Add.Invoke(from, new Point3D(p), new Point3D(p), _ctor);
            from.Target = new InternalTarget(_ctor, _searchResults, _searchString, _page);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            if (cancelType == TargetCancelType.Canceled)
            {
                from.SendGump(new AddGump(_searchString, _page, _searchResults, true));
            }
        }
    }
}
