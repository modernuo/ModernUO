using System;
using System.Collections.Generic;

namespace Server.Items;

public partial class FillableContent
{
    private readonly FillableEntry[] _entries;
    private readonly int _weight;

    public FillableContent(int level, Type[] vendors, FillableEntry[] entries)
    {
        Level = level;
        Vendors = vendors;
        _entries = entries;

        for (var i = 0; i < entries.Length; ++i)
        {
            _weight += entries[i].Weight;
        }
    }

    public int Level { get; }

    public Type[] Vendors { get; }

    public FillableContentType TypeID => Lookup(this);

    public virtual Item Construct()
    {
        var index = Utility.Random(_weight);

        for (var i = 0; i < _entries.Length; ++i)
        {
            var entry = _entries[i];

            if (index < entry.Weight)
            {
                return entry.Construct();
            }

            index -= entry.Weight;
        }

        return null;
    }

    public static FillableContent Lookup(FillableContentType type)
    {
        var v = (int)type;

        if (v >= 0 && v < ContentTypes.Length)
        {
            return ContentTypes[v];
        }

        return null;
    }

    public static FillableContentType Lookup(FillableContent content)
    {
        if (content == null)
        {
            return FillableContentType.None;
        }

        return (FillableContentType)Array.IndexOf(ContentTypes, content);
    }

    public static FillableContentType Acquire(Point3D loc, Map map)
    {
        FillableContentType content = FillableContentType.None;

        if (map == null || map == Map.Internal)
        {
            return content;
        }

        if (_acquireTable == null)
        {
            _acquireTable = new Dictionary<Type, FillableContentType>();

            for (var i = 0; i < ContentTypes.Length; ++i)
            {
                var fill = ContentTypes[i];

                for (var j = 0; j < fill.Vendors.Length; ++j)
                {
                    _acquireTable[fill.Vendors[j]] = fill.TypeID;
                }
            }
        }

        Mobile nearest = null;

        // TODO: Replace with vendor shop regions and a fallback override.
        foreach (var mob in map.GetMobilesInRange(loc, 20))
        {
            if (nearest != null && mob.GetDistanceToSqrt(loc) > nearest.GetDistanceToSqrt(loc) &&
                !(nearest is Mobiles.Cobbler && mob is Mobiles.Provisioner))
            {
                continue;
            }

            if (_acquireTable.TryGetValue(mob.GetType(), out var check))
            {
                nearest = mob;
                content = check;
            }
        }

        return content;
    }
}
