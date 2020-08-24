/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: UnicodePrompt.cs - Created: 2020/05/08 - Updated: 2020/05/08    *
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

using Server.Prompts;

namespace Server.Network
{
    public sealed class UnicodePrompt : Packet
    {
        public UnicodePrompt(Prompt prompt) : base(0xC2)
        {
            EnsureCapacity(21);

            Stream.Write(prompt.Serial); // TODO: Does this value even matter?
            Stream.Write(prompt.Serial);
            Stream.Write(0);
            Stream.Write(0);
            Stream.Write((short)0);
        }
    }
}
