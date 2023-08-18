/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MLQuestPackets.cs                                               *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Network;

namespace Server.Engines.MLQuests
{
    public static class MLQuestPackets
    {
        public static unsafe void SendRaceChanger(this NetState ns, bool female, Race newRace)
        {
            ns?.Send(stackalloc byte[] { 0xBF, 0x00, 0x07, 0x00, 0x2A, *(byte*)&female, (byte)(newRace.RaceID + 1) });
        }

        public static void SendCloseRaceChanger(this NetState ns)
        {
            ns?.Send(stackalloc byte[] { 0xBF, 0x00, 0x07, 0x00, 0x2A, 0x0, 0xFF });
        }
    }
}
