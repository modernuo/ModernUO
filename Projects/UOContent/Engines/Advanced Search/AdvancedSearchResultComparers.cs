using System.Collections.Generic;

namespace Server.Engines.AdvancedSearch;

// Results arrive in worker-finish order, so every sort ends in the same serial tie-break.
public abstract class AdvancedSearchResultComparer : IComparer<AdvancedSearchResult>
{
    protected readonly bool Reverse;

    protected AdvancedSearchResultComparer(bool reverse) => Reverse = reverse;

    public int Compare(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x == null)
        {
            return -1;
        }

        if (y == null)
        {
            return 1;
        }

        var c = CompareKey(x, y);

        return c != 0 ? c : CompareSerial(x, y);
    }

    protected abstract int CompareKey(AdvancedSearchResult x, AdvancedSearchResult y);

    // Always ascending; the direction applies to the key only.
    public static int CompareSerial(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        var a = x.Entity?.Serial ?? Serial.Zero;
        var b = y.Entity?.Serial ?? Serial.Zero;

        return a.CompareTo(b);
    }
}

public sealed class AdvancedSearchResultSerialComparer : AdvancedSearchResultComparer
{
    public static readonly AdvancedSearchResultSerialComparer Instance = new();

    private AdvancedSearchResultSerialComparer() : base(false)
    {
    }

    protected override int CompareKey(AdvancedSearchResult x, AdvancedSearchResult y) => 0;
}

public sealed class AdvancedSearchResultTypeComparer : AdvancedSearchResultComparer
{
    public static readonly AdvancedSearchResultTypeComparer Instance = new();
    public static readonly AdvancedSearchResultTypeComparer InstanceReverse = new(true);

    public AdvancedSearchResultTypeComparer(bool reverse = false) : base(reverse)
    {
    }

    protected override int CompareKey(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        var a = x.Entity?.GetType().Name;
        var b = y.Entity?.GetType().Name;

        return Reverse ? b.InsensitiveCompare(a) : a.InsensitiveCompare(b);
    }
}

public sealed class AdvancedSearchResultNameComparer : AdvancedSearchResultComparer
{
    public static readonly AdvancedSearchResultNameComparer Instance = new();
    public static readonly AdvancedSearchResultNameComparer InstanceReverse = new(true);

    public AdvancedSearchResultNameComparer(bool reverse = false) : base(reverse)
    {
    }

    protected override int CompareKey(AdvancedSearchResult x, AdvancedSearchResult y) =>
        Reverse ? y.Name.InsensitiveCompare(x.Name) : x.Name.InsensitiveCompare(y.Name);
}

public sealed class AdvancedSearchResultMapComparer : AdvancedSearchResultComparer
{
    public static readonly AdvancedSearchResultMapComparer Instance = new();
    public static readonly AdvancedSearchResultMapComparer InstanceReverse = new(true);

    public AdvancedSearchResultMapComparer(bool reverse = false) : base(reverse)
    {
    }

    protected override int CompareKey(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        var a = x.Map?.MapID ?? -1;
        var b = y.Map?.MapID ?? -1;

        return Reverse ? b.CompareTo(a) : a.CompareTo(b);
    }
}

public sealed class AdvancedSearchRangeComparer : AdvancedSearchResultComparer
{
    private readonly Mobile _from;

    public AdvancedSearchRangeComparer(Mobile from, bool reverse = false) : base(reverse) => _from = from;

    protected override int CompareKey(AdvancedSearchResult x, AdvancedSearchResult y)
    {
        if (_from == null)
        {
            return 0;
        }

        var fromMap = _from.Map;

        if (x.Map != fromMap && y.Map != fromMap)
        {
            return 0;
        }

        if (x.Map == fromMap && y.Map != fromMap)
        {
            return Reverse ? 1 : -1;
        }

        if (x.Map != fromMap && y.Map == fromMap)
        {
            return Reverse ? -1 : 1;
        }

        var xDist = _from.GetDistanceToSqrt(x.Location);
        var yDist = _from.GetDistanceToSqrt(y.Location);

        return Reverse ? yDist.CompareTo(xDist) : xDist.CompareTo(yDist);
    }
}

public sealed class AdvancedSearchResultSelectedComparer : AdvancedSearchResultComparer
{
    public static readonly AdvancedSearchResultSelectedComparer Instance = new();
    public static readonly AdvancedSearchResultSelectedComparer InstanceReverse = new(true);

    public AdvancedSearchResultSelectedComparer(bool reverse = false) : base(reverse)
    {
    }

    // True then false, which is 1 then 0, so the comparison is reverse of integers
    protected override int CompareKey(AdvancedSearchResult x, AdvancedSearchResult y) =>
        Reverse ? x.Selected.CompareTo(y.Selected) : y.Selected.CompareTo(x.Selected);
}
