/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PacketUtilities.cs                                              *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers;
using System.IO;

namespace Server.Network
{
    public static class PacketUtilities
    {
        public static void WritePacketLength(this CircularBufferWriter writer)
        {
            var length = writer.Position;
            writer.Seek(1, SeekOrigin.Begin);
            writer.Write((ushort)length);
            writer.Seek(length, SeekOrigin.Begin);
        }
    }
}
