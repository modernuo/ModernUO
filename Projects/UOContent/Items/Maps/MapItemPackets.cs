/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MapItemPackets.cs                                               *
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
using Server.Items;

namespace Server.Network
{
    public static class MapItemPackets
    {
        public static unsafe void Configure()
        {
            IncomingPackets.Register(0x56, 11, true, &OnMapCommand);
        }

        private static void OnMapCommand(NetState state, CircularBufferReader reader, int packetLength)
        {
            var from = state.Mobile;

            if (World.FindItem((Serial)reader.ReadUInt32()) is not MapItem map)
            {
                return;
            }

            int command = reader.ReadByte();
            int number = reader.ReadByte();

            int x = reader.ReadInt16();
            int y = reader.ReadInt16();

            switch (command)
            {
                case 1:
                    map.OnAddPin(from, x, y);
                    break;
                case 2:
                    map.OnInsertPin(from, number, x, y);
                    break;
                case 3:
                    map.OnChangePin(from, number, x, y);
                    break;
                case 4:
                    map.OnRemovePin(from, number);
                    break;
                case 5:
                    map.OnClearPins(from);
                    break;
                case 6:
                    map.OnToggleEditable(from);
                    break;
            }
        }

        public static void SendMapDetails(this NetState ns, MapItem map)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[ns.NewCharacterList ? 21 : 19]);
            writer.Write((byte)(ns.NewCharacterList ? 0xF5 : 0x90)); // Packet ID
            writer.Write(map.Serial);
            writer.Write((short)0x139D);

            var bounds = map.Bounds;
            writer.Write((short)bounds.Start.X);
            writer.Write((short)bounds.Start.Y);
            writer.Write((short)bounds.End.X);
            writer.Write((short)bounds.End.Y);
            writer.Write((short)map.Width);
            writer.Write((short)map.Height);

            if (ns.NewCharacterList)
            {
                writer.Write((short)(map.Facet?.MapID ?? 0));
            }

            ns.Send(writer.Span);
        }

        public static void SendMapCommand(this NetState ns, MapItem map, int command, int x = 0, int y = 0, bool editable = false)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[11]);
            writer.Write((byte)0x56); // Packet ID
            writer.Write(map.Serial);
            writer.Write((byte)command);
            writer.Write(editable);
            writer.Write((short)x);
            writer.Write((short)y);

            ns.Send(writer.Span);
        }

        public static void SendMapDisplay(this NetState ns, MapItem map) => ns.SendMapCommand(map, 5);
        public static void SendMapAddPin(this NetState ns, MapItem map, Point2D p) => ns.SendMapCommand(map, 1, p.X, p.Y);
        public static void SendMapSetEditable(this NetState ns, MapItem map, bool editable) =>
            ns.SendMapCommand(map, 7, 0, 0, true);
    }
}
