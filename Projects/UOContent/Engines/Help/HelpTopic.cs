/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: HelpTopic.cs                                                    *
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
using Server.Network;

namespace Server.Engines.Help
{
    public static class HelpTopic
    {
        // Topics
        public const int Healing = 29;
        public const int PlantGrowing = 48;
        public const int InfestationLevel = 54;
        public const int FungusLevel = 56;
        public const int PoisonLevel = 58;
        public const int DiseaseLevel = 60;
        public const int PollinationState = 67;
        public const int SeedProduction = 68;
        public const int ResourceProduction = 69;
        public const int DecorativeMode = 70;
        public const int EmptyingBowl = 71;

        public static void SendDisplayHelpTopic(this NetState ns, int topicID, bool display = true)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[11]);
            writer.Write((byte)0xBF); // Packet ID
            writer.Write((ushort)11);
            writer.Write((ushort)0x17); // Sub-packet
            writer.Write((byte)1); // Command

            writer.Write(topicID);
            writer.Write(display);

            ns.Send(writer.Span);
        }
    }
}
