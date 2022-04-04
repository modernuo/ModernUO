/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingHousePackets.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network;

public static class IncomingHousePackets
{
    public static void Configure()
    {
        IncomingPackets.Register(0xFB, 2, false, ShowPublicHouseContent);
    }

    public static void ShowPublicHouseContent(NetState state, CircularBufferReader reader, int packetLength)
    {
        var showPublicHouseContent = reader.ReadBoolean();
    }
}
