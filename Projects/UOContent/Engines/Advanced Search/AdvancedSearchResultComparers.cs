using System.Collections.Generic;

namespace Server.Engines.AdvancedSearch;

public class AdvancedSearchResultTypeComparer : IComparer<AdvancedSearchResult>
{
    public static readonly AdvancedSearchResultTypeComparer Instance = new();
    public static readonly AdvancedSearchResultTypeComparer InstanceReverse = new(true);

    private readonly bool _reverse;

    public AdvancedSearchResultTypeComparer(bool reverse = false) => _reverse = reverse;

    public int Compare(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        var a = x?.Entity?.GetType().Name;
        var b = y?.Entity?.GetType().Name;

        return _reverse ? b.InsensitiveCompare(a) : a.InsensitiveCompare(b);
    }
}

public class AdvancedSearchResultNameComparer : IComparer<AdvancedSearchResult>
{
    public static readonly AdvancedSearchResultNameComparer Instance = new();
    public static readonly AdvancedSearchResultNameComparer InstanceReverse = new(true);

    private readonly bool _reverse;

    public AdvancedSearchResultNameComparer(bool reverse = false) => _reverse = reverse;

    public int Compare(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        var a = x?.Name;
        var b = y?.Name;

        return _reverse ? b.InsensitiveCompare(a) : a.InsensitiveCompare(b);
    }
}

public class AdvancedSearchResultMapComparer : IComparer<AdvancedSearchResult>
{
    public static readonly AdvancedSearchResultMapComparer Instance = new();
    public static readonly AdvancedSearchResultMapComparer InstanceReverse = new(true);

    private readonly bool _reverse;

    public AdvancedSearchResultMapComparer(bool reverse = false) => _reverse = reverse;

    public int Compare(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        var a = x?.Map?.MapID ?? -1;
        var b = y?.Map?.MapID ?? -1;

        return _reverse ? b.CompareTo(a) : a.CompareTo(b);
    }
}

public class AdvancedSearchRangeComparer : IComparer<AdvancedSearchResult>
{
    private readonly bool _reverse;
    private readonly Mobile _from;

    public AdvancedSearchRangeComparer(Mobile from, bool reverse = false)
    {
        _from = from;
        _reverse = reverse;
    }

    public int Compare(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        if (_from == null || x == null && y == null)
        {
            return 0;
        }

        if (x == null)
        {
            return _reverse ? 1 : -1;
        }

        if (y == null)
        {
            return _reverse ? -1 : 1;
        }

        var fromMap = _from.Map;

        if (x.Map != fromMap && y.Map != fromMap)
        {
            return 0;
        }

        if (x.Map == fromMap && y.Map != fromMap)
        {
            return _reverse ? 1 : -1;
        }

        if (x.Map != fromMap && y.Map == fromMap)
        {
            return _reverse ? -1 : 1;
        }

        var xDist = _from.GetDistanceToSqrt(x.Location);
        var yDist = _from.GetDistanceToSqrt(y.Location);

        return _reverse ? yDist.CompareTo(xDist) : xDist.CompareTo(yDist);
    }
}

public class AdvancedSearchResultSelectedComparer : IComparer<AdvancedSearchResult>
{
    public static readonly AdvancedSearchResultSelectedComparer Instance = new();
    public static readonly AdvancedSearchResultSelectedComparer InstanceReverse = new(true);

    private readonly bool _reverse;

    public AdvancedSearchResultSelectedComparer(bool reverse = false) => _reverse = reverse;

    public int Compare(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        var a = x?.Selected ?? false;
        var b = y?.Selected ?? false;

        // True then false, which is 1 then 0, so the comparison is reverse of integers
        return _reverse ? a.CompareTo(b) : b.CompareTo(a);
    }
}
