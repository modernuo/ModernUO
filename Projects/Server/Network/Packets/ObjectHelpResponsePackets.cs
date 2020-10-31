/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ObjectHelpResponsePackets.cs                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
    public sealed class ObjectHelpResponse : Packet
    {
        public ObjectHelpResponse(Serial e, string text) : base(0xB7)
        {
            EnsureCapacity(9 + text.Length * 2);

            Stream.Write(e);
            Stream.WriteBigUniNull(text);
        }
    }
}
