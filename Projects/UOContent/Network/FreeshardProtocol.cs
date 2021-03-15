/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: FreeshardProtocol.cs                                            *
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
    public static class FreeshardProtocol
    {
        private static PacketHandler[] _handlers;

        [CallPriority(10)]
        public static void Configure()
        {
            _handlers = ProtocolExtensions.Register(0xF1);
        }

        public static void Register(int cmd, bool ingame, OnPacketReceive onReceive) =>
            _handlers[cmd] = new PacketHandler(cmd, 0, ingame, onReceive);
    }
}
