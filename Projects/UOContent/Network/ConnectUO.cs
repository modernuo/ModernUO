/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: ConnectUO.cs                                                    *
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
using Server.Accounting;

namespace Server.Network
{
    public static class ConnectUOServerPoller
    {
        public const byte ConnectUOProtocolVersion = 0;
        public const byte ConnectUOServerType = 5;
        private static long _serverStart;

        public static void Configure()
        {
            _serverStart = Core.TickCount;
            var enabled = ServerConfiguration.GetOrUpdateSetting("connectuo.enabled", true);
            if (enabled)
            {
                FreeshardProtocol.Register(0xC0, false, PollInfo);
            }
        }

        public static void PollInfo(NetState ns, CircularBufferReader reader, ref int packetLength)
        {
            var version = reader.ReadByte();
            ns.WriteConsole($"ConnectUO (v{version}) is requesting stats.");
            ns.SendServerPollInfo();
        }

        public static void SendServerPollInfo(this NetState ns)
        {
            if (ns == null)
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[15]);
            writer.Write((byte)0xC0); // Packet ID
            writer.Write(17); // Length
            writer.Write(ConnectUOProtocolVersion); // Version
            writer.Write(ConnectUOServerType);
            writer.Write((int)(Core.TickCount - _serverStart) / 1000);
            writer.Write(Accounts.Count);
            writer.Write(TcpServer.Instances.Count);

            ns.Send(writer.Span);
        }
    }
}
