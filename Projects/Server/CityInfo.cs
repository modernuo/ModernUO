/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: CityInfo.cs - Created: 2019/10/04 - Updated: 2020/01/19         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server
{
    public sealed class CityInfo
    {
        private Point3D m_Location;

        public CityInfo(string city, string building, int description, int x, int y, int z, Map m)
        {
            City = city;
            Building = building;
            Description = description;
            Location = new Point3D(x, y, z);
            Map = m;
        }

        public CityInfo(string city, string building, int x, int y, int z, Map m) : this(city, building, 0, x, y, z, m)
        {
        }

        public CityInfo(string city, string building, int description, int x, int y, int z) : this(
            city,
            building,
            description,
            x,
            y,
            z,
            Map.Trammel
        )
        {
        }

        public CityInfo(string city, string building, int x, int y, int z) : this(city, building, 0, x, y, z, Map.Trammel)
        {
        }

        public string City { get; set; }

        public string Building { get; set; }

        public int Description { get; set; }

        public int X
        {
            get => m_Location.X;
            set => m_Location.X = value;
        }

        public int Y
        {
            get => m_Location.Y;
            set => m_Location.Y = value;
        }

        public int Z
        {
            get => m_Location.Z;
            set => m_Location.Z = value;
        }

        public Point3D Location
        {
            get => m_Location;
            set => m_Location = value;
        }

        public Map Map { get; set; }
    }
}
