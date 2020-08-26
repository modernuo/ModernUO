/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: CombatPackets.cs - Created: 2020/06/25 - Updated: 2020/06/25    *
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
    public sealed class Swing : Packet
    {
        public Swing(Serial attacker, Serial defender) : base(0x2F, 10)
        {
            Stream.Write((byte)0);
            Stream.Write(attacker);
            Stream.Write(defender);
        }
    }

    public sealed class SetWarMode : Packet
    {
        public static readonly Packet InWarMode = SetStatic(new SetWarMode(true));
        public static readonly Packet InPeaceMode = SetStatic(new SetWarMode(false));

        public SetWarMode(bool mode) : base(0x72, 5)
        {
            Stream.Write(mode);
            Stream.Write((byte)0x00);
            Stream.Write((byte)0x32);
            Stream.Write((byte)0x00);
        }

        public static Packet Instantiate(bool mode) => mode ? InWarMode : InPeaceMode;
    }

    public sealed class ChangeCombatant : Packet
    {
        public ChangeCombatant(Serial combatant) : base(0xAA, 5)
        {
            Stream.Write(combatant);
        }
    }
}
