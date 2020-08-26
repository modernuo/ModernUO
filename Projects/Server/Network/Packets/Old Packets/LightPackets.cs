/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: LightPackets.cs - Created: 2020/06/25 - Updated: 2020/06/25     *
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

namespace Server.Network
{
    public sealed class GlobalLightLevel : Packet
    {
        private static readonly GlobalLightLevel[] m_Cache = new GlobalLightLevel[0x100];

        public GlobalLightLevel(int level) : base(0x4F, 2)
        {
            Stream.Write((sbyte)level);
        }

        public static GlobalLightLevel Instantiate(int level)
        {
            var lvl = (byte)level;
            var p = m_Cache[lvl];

            if (p == null)
            {
                m_Cache[lvl] = p = new GlobalLightLevel(level);
                p.SetStatic();
            }

            return p;
        }
    }

    public sealed class PersonalLightLevel : Packet
    {
        public PersonalLightLevel(Serial mobile, int level = 0) : base(0x4E, 6)
        {
            Stream.Write(mobile);
            Stream.Write((sbyte)level);
        }
    }
}
