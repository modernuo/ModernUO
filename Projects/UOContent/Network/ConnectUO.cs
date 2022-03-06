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
using Server.Logging;
using Server.Text;

namespace Server.Network
{
    public static class ConnectUO
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(ConnectUO));

        public enum ConnectUOServerType
        {
            RunUO,
            ServUO,
            UOX3,
            POL,
            Sphere,
            ModernUO
        }

        public const byte ConnectUOProtocolVersion = 0;
        private const int _connectUOTokenLength = 32;
        private const ConnectUOServerType _serverType = ConnectUOServerType.ModernUO;
        private static byte[] _token;

        public static void Configure()
        {
            var enabled = ServerConfiguration.GetOrUpdateSetting("connectuo.enabled", true);
            var token = ServerConfiguration.GetOrUpdateSetting("connectuo.token", "");

            if (enabled)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        if (token.Length != _connectUOTokenLength * 2)
                        {
                            throw new Exception("Invalid length for ConnectUO token");
                        }

                        _token = GC.AllocateUninitializedArray<byte>(_connectUOTokenLength);
                        token.ToUpperInvariant().GetBytes(_token);
                    }
                }
                catch
                {
                    logger.Warning("ConnectUO token could not be parsed. Make sure modernuo.json is properly configured");
                    _token = null;
                }

                FreeshardProtocol.Register(0xC0, false, PollInfo);
            }
        }

        public static void PollInfo(NetState state, CircularBufferReader reader, int packetLength)
        {
            var version = reader.ReadByte();

            if (_token != null)
            {
                unsafe {
                    byte* tok = stackalloc byte[_token.Length];
                    var span = new Span<byte>(tok, _token.Length);
                    reader.Read(span);

                    if (!span.SequenceEqual(_token))
                    {
                        state.Disconnect("Invalid token sent for ConnectUO");
                        return;
                    }
                }
            }

            state.LogInfo($"ConnectUO (v{version}) is requesting stats.");
            if (version > ConnectUOProtocolVersion)
            {
                Utility.PushColor(ConsoleColor.Yellow);
                state.LogInfo("Warning! ConnectUO (v{version}) is newer than what is supported.");
                Utility.PopColor();
            }

            state.SendServerPollInfo();
        }

        public static void SendServerPollInfo(this NetState ns)
        {
            if (ns.CannotSendPackets())
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[15]);
            writer.Write((byte)0xC0); // Packet ID
            writer.Write((ushort)17); // Length
            writer.Write(ConnectUOProtocolVersion); // Version
            writer.Write((byte)_serverType);
            writer.Write((int)(Core.TickCount / 1000));
            writer.Write(Accounts.Count); // Shame if you modify this!
            writer.Write(TcpServer.Instances.Count - 1); // Shame if you modify this!

            ns.Send(writer.Span);
        }
    }
}
