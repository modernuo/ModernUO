/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RegionJsonDto.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Text.Json.Serialization;
using Server.Utilities;

namespace Server;

public class RegionJsonDto
{
    public Expansion MinExpansion { get; set; } = Expansion.None;

    public Expansion? MaxExpansion { get; set; } = Expansion.EJ;

    [JsonIgnore]
    public virtual Type RegionType => typeof(Region);

    public Map Map { get; set; }
    public string? Parent { get; set; }
    public string Name { get; set; }
    public int? Priority { get; set; }
    public Rectangle3D[] Area { get; set; }
    public Point3D GoLocation { get; set; }
    public MusicName? Music { get; set; }

    public virtual void FromRegion(Region region)
    {
        Map = region.Map;
        Parent = region.Parent?.Name;
        Name = region.Name;
        Priority = region.Priority;
        Area = region.Area;
        GoLocation = region.GoLocation;
        Music = region.Music != MusicName.Invalid && region.Music != region.DefaultMusic ? region.Music : null;
        MaxExpansion = region.MaxExpansion == Expansion.EJ ? null : region.MaxExpansion;
        MinExpansion = region.MinExpansion;
    }

    public Region ToRegion()
    {
        var region = Priority != null
            ? RegionType.CreateInstance<Region>(Name, Map, Region.Find(Parent, Map), Priority, Area)
            : RegionType.CreateInstance<Region>(Name, Map, Region.Find(Parent, Map), Area);

        HydrateRegion(region);
        return region;
    }

    protected virtual void HydrateRegion(Region region)
    {
        region.GoLocation = GoLocation;
        region.Music = Music ?? region.DefaultMusic;
    }
}
