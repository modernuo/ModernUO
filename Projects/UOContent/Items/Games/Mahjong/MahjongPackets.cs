/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2021 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MahjongPackets.cs                                               *
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
using System.IO;
using Server.Network;

namespace Server.Engines.Mahjong
{
    public static class MahjongPackets
    {
        public const int MahjongGeneralInfoPacketLength = 25;
        public const int MahjongRelievePacketLength = 9;

        public static void SendMahjongJoinGame(this NetState ns, Serial game)
        {
            if (ns == null)
            {
                return;
            }

            var writer = new SpanWriter(stackalloc byte[9]);
            writer.Write((byte)0xDA); // Packet ID
            writer.Write((ushort)9);
            writer.Write(game);
            writer.Write((ushort)0x19); // Command

            ns.Send(writer.Span);
        }

        public static void SendMahjongPlayersInfo(this NetState ns, MahjongGame game, Mobile to)
        {
            if (ns == null)
            {
                return;
            }

            var maxLength = 11 + game.Players.Seats * 45;
            var writer = new SpanWriter(stackalloc byte[maxLength]);
            writer.Write((byte)0xDA); // Packet ID
            writer.Seek(2, SeekOrigin.Current);
            writer.Write(game.Serial);
            writer.Write((ushort)0x02); // Command

            writer.Seek(2, SeekOrigin.Current); // Seats

            var players = game.Players;
            var count = 0;

            for (var i = 0; i < players.Seats; i++)
            {
                var m = players.GetPlayer(i);

                if (m == null && !game.ShowScores)
                {
                    continue;
                }

                writer.Write(m?.Serial ?? Serial.Zero);
                writer.Write(m != null && players.DealerPosition == i ? (byte)0x1 : (byte)0x2);
                writer.Write((byte)i);

                if (game.ShowScores || m == to)
                {
                    writer.Write(players.GetScore(i));
                }
                else
                {
                    writer.Write(0);
                }

                writer.Write((short)0);
                writer.Write((byte)0);

                writer.Write(players.IsPublic(i));
                writer.WriteAscii(m?.Name ?? "", 30);

                writer.Write(m == null || !players.IsInGamePlayer(i));
                count++;
            }

            writer.Seek(9, SeekOrigin.Begin);
            writer.Write((ushort)count);
            writer.WritePacketLength();

            ns.Send(writer.Span);
        }

        public static void SendMahjongTileInfo(this NetState ns, MahjongTile tile, Mobile to)
        {
            if (ns == null)
            {
                return;
            }

            var game = tile.Game;
            var players = game.Players;

            var writer = new SpanWriter(stackalloc byte[18]);
            writer.Write((byte)0xDA); // Packet ID
            writer.Write((ushort)18);
            writer.Write(game.Serial);
            writer.Write((ushort)0x03); // Command

            writer.Write((byte)tile.Number);

            if (tile.Flipped)
            {
                var hand = tile.Dimensions.GetHandArea();

                if (hand < 0 || players.IsPublic(hand) || players.GetPlayer(hand) == to ||
                    game.SpectatorVision && players.IsSpectator(to))
                {
                    writer.Write((byte)tile.Value);
                }
                else
                {
                    writer.Write((byte)0);
                }
            }
            else
            {
                writer.Write((byte)0);
            }

            writer.Write((short)tile.Position.Y);
            writer.Write((short)tile.Position.X);
            writer.Write((byte)tile.StackLevel);
            writer.Write((byte)tile.Direction);
            writer.Write((byte)(tile.Flipped ? 0x10 : 0x0));

            ns.Send(writer.Span);
        }

        public static void SendMahjongTilesInfo(this NetState ns, MahjongGame game, Mobile to)
        {
            if (ns == null)
            {
                return;
            }

            var tiles = game.Tiles;
            var players = game.Players;

            var length = 11 + tiles.Length * 9;
            var writer = new SpanWriter(stackalloc byte[length]);
            writer.Write((byte)0xDA); // Packet ID
            writer.Write((ushort)length);
            writer.Write(game.Serial);
            writer.Write((ushort)0x04); // Command

            writer.Write((short)tiles.Length);

            foreach (var tile in tiles)
            {
                writer.Write((byte)tile.Number);

                if (tile.Flipped)
                {
                    var hand = tile.Dimensions.GetHandArea();

                    if (hand < 0 || players.IsPublic(hand) || players.GetPlayer(hand) == to ||
                        game.SpectatorVision && players.IsSpectator(to))
                    {
                        writer.Write((byte)tile.Value);
                    }
                    else
                    {
                        writer.Write((byte)0);
                    }
                }
                else
                {
                    writer.Write((byte)0);
                }

                writer.Write((short)tile.Position.Y);
                writer.Write((short)tile.Position.X);
                writer.Write((byte)tile.StackLevel);
                writer.Write((byte)tile.Direction);

                writer.Write((byte)(tile.Flipped ? 0x10 : 0x0));
            }

            ns.Send(writer.Span);
        }

        public static void SendMahjongGeneralInfo(this NetState ns, MahjongGame game)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[MahjongGeneralInfoPacketLength].InitializePacket();
            CreateMahjongGeneralInfo(buffer, game);

            ns.Send(buffer);
        }

        public static void CreateMahjongGeneralInfo(Span<byte> buffer, MahjongGame game)
        {
            if (buffer[0] != 0)
            {
                return;
            }

            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xDA); // Packet ID
            writer.Write((ushort)MahjongGeneralInfoPacketLength);
            writer.Write(game.Serial);
            writer.Write((ushort)0x05); // Command

            writer.Write((short)0);
            writer.Write((byte)0);

            writer.Write((byte)((game.ShowScores ? 0x1 : 0x0) | (game.SpectatorVision ? 0x2 : 0x0)));

            writer.Write((byte)game.Dices.First);
            writer.Write((byte)game.Dices.Second);

            writer.Write((byte)game.DealerIndicator.Wind);
            writer.Write((short)game.DealerIndicator.Position.Y);
            writer.Write((short)game.DealerIndicator.Position.X);
            writer.Write((byte)game.DealerIndicator.Direction);

            writer.Write((short)game.WallBreakIndicator.Position.Y);
            writer.Write((short)game.WallBreakIndicator.Position.X);
        }

        public static void SendMahjongRelieve(this NetState ns, Serial game)
        {
            if (ns == null)
            {
                return;
            }

            Span<byte> buffer = stackalloc byte[MahjongRelievePacketLength].InitializePacket();
            CreateMahjongRelieve(buffer, game);

            ns.Send(buffer);
        }

        public static void CreateMahjongRelieve(Span<byte> buffer, Serial game)
        {
            if (buffer[0] != 0)
            {
                return;
            }

            var writer = new SpanWriter(buffer);
            writer.Write((byte)0xDA); // Packet ID
            writer.Write((ushort)MahjongRelievePacketLength);
            writer.Write(game);
            writer.Write((ushort)0x1A); // Command
        }
    }
}
