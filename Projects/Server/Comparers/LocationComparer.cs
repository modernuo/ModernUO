/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LocationComparer.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;

namespace Server;

public class LocationComparer : IComparer<IPoint3D>
{
    private static LocationComparer m_Instance;

    public LocationComparer(IPoint3D relativeTo) => RelativeTo = relativeTo;

    public IPoint3D RelativeTo { get; set; }

    public int Compare(IPoint3D x, IPoint3D y) => GetDistance(x) - GetDistance(y);

    public static LocationComparer GetInstance(IPoint3D relativeTo)
    {
        if (m_Instance == null)
        {
            m_Instance = new LocationComparer(relativeTo);
        }
        else
        {
            m_Instance.RelativeTo = relativeTo;
        }

        return m_Instance;
    }

    private int GetDistance(IPoint3D p)
    {
        var x = RelativeTo.X - p.X;
        var y = RelativeTo.Y - p.Y;
        var z = RelativeTo.Z - p.Z;

        x *= 11;
        y *= 11;

        return x * x + y * y + z * z;
    }
}
