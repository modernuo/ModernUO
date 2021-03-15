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

using System;
using System.Buffers;
using Server.Accounting;
using Server.Text;

namespace Server.Network
{
    public static class ConnectUOServerPoller
    {
        public const byte ConnectUOProtocolVersion = 0;
        public const byte ConnectUOServerType = 5;
        private static long _serverStart;
        private static byte[] _token;

        public static void Configure()
        {
            _serverStart = Core.TickCount;
            var enabled = ServerConfiguration.GetOrUpdateSetting("connectuo.enabled", true);
            var token = ServerConfiguration.GetOrUpdateSetting("connectuo.token", null);

            if (enabled)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        _token = GC.AllocateUninitializedArray<byte>(32);
                        token.GetBytes(_token);
                    }
                }
                catch
                {
                    Utility.PushColor(ConsoleColor.Red);
                    Console.WriteLine("ConnectUO token could not be parsed");
                    Console.WriteLine("Make sure modernuo.json is properly configured");
                    Utility.PopColor();
                    _token = null;
                }

                FreeshardProtocol.Register(0xC0, false, PollInfo);
            }
        }

        public static void PollInfo(NetState ns, CircularBufferReader reader, ref int packetLength)
        {
            var version = reader.ReadByte();

            if (_token != null)
            {
                unsafe {
                    byte* tok = stackalloc byte[32];
                    var span = new Span<byte>(tok, 32);
                    reader.Read(span);

                    if (!span.SequenceEqual(_token))
                    {
                        ns.Disconnect("Invalid token sent for ConnectUO");
                        return;
                    }
                }
            }

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
