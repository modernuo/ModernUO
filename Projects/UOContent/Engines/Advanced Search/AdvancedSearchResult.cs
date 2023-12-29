using System;

namespace Server.Engines.AdvancedSearch;

public record AdvancedSearchResult(string Name, Type Type, Point3D Location, Map Map, IEntity Parent)
{
    public IEntity Entity { get; set; }
    public bool Selected { get; set; }

    public Point3D GetLocation()
    {
        if (Parent?.Deleted == false)
        {
            return Parent.Location;
        }

        if (Entity?.Deleted == false)
        {
            return Entity.Location;
        }

        return Location;
    }

    public Map GetMap()
    {
        if (Parent?.Deleted == false)
        {
            return Parent.Map;
        }

        if (Entity?.Deleted == false)
        {
            return Entity.Map;
        }

        return Map;
    }
}
