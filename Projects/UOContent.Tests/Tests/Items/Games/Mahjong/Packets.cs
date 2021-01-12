using System.IO;
using Server.Network;

namespace Server.Engines.Mahjong
{
    public sealed class MahjongJoinGame : Packet
    {
        public MahjongJoinGame(Serial game) : base(0xDA)
        {
            EnsureCapacity(9);

            Stream.Write(game);
            Stream.Write((byte)0);
            Stream.Write((byte)0x19);
        }
    }

    public sealed class MahjongPlayersInfo : Packet
    {
        public MahjongPlayersInfo(MahjongGame game, Mobile to) : base(0xDA)
        {
            var players = game.Players;

            EnsureCapacity(11 + 45 * players.Seats);

            Stream.Write(game.Serial);
            Stream.Write((byte)0);
            Stream.Write((byte)0x2);

            Stream.Write((byte)0);
            Stream.Write((byte)players.Seats);

            var n = 0;
            for (var i = 0; i < players.Seats; i++)
            {
                var mobile = players.GetPlayer(i);

                if (mobile != null)
                {
                    Stream.Write(mobile.Serial);
                    Stream.Write(players.DealerPosition == i ? (byte)0x1 : (byte)0x2);
                    Stream.Write((byte)i);

                    if (game.ShowScores || mobile == to)
                    {
                        Stream.Write(players.GetScore(i));
                    }
                    else
                    {
                        Stream.Write(0);
                    }

                    Stream.Write((short)0);
                    Stream.Write((byte)0);

                    Stream.Write(players.IsPublic(i));

                    Stream.WriteAsciiFixed(mobile.Name, 30);
                    Stream.Write(!players.IsInGamePlayer(i));

                    n++;
                }
                else if (game.ShowScores)
                {
                    Stream.Write(0);
                    Stream.Write((byte)0x2);
                    Stream.Write((byte)i);

                    Stream.Write(players.GetScore(i));

                    Stream.Write((short)0);
                    Stream.Write((byte)0);

                    Stream.Write(players.IsPublic(i));

                    Stream.WriteAsciiFixed("", 30);
                    Stream.Write(true);

                    n++;
                }
            }

            if (n != players.Seats)
            {
                Stream.Seek(10, SeekOrigin.Begin);
                Stream.Write((byte)n);
            }
        }
    }

    public sealed class MahjongGeneralInfo : Packet
    {
        public MahjongGeneralInfo(MahjongGame game) : base(0xDA)
        {
            EnsureCapacity(13);

            Stream.Write(game.Serial);
            Stream.Write((byte)0);
            Stream.Write((byte)0x5);

            Stream.Write((short)0);
            Stream.Write((byte)0);

            Stream.Write((byte)((game.ShowScores ? 0x1 : 0x0) | (game.SpectatorVision ? 0x2 : 0x0)));

            Stream.Write((byte)game.Dices.First);
            Stream.Write((byte)game.Dices.Second);

            Stream.Write((byte)game.DealerIndicator.Wind);
            Stream.Write((short)game.DealerIndicator.Position.Y);
            Stream.Write((short)game.DealerIndicator.Position.X);
            Stream.Write((byte)game.DealerIndicator.Direction);

            Stream.Write((short)game.WallBreakIndicator.Position.Y);
            Stream.Write((short)game.WallBreakIndicator.Position.X);
        }
    }

    public sealed class MahjongTilesInfo : Packet
    {
        public MahjongTilesInfo(MahjongGame game, Mobile to) : base(0xDA)
        {
            var tiles = game.Tiles;
            var players = game.Players;

            EnsureCapacity(11 + 9 * tiles.Length);

            Stream.Write(game.Serial);
            Stream.Write((byte)0);
            Stream.Write((byte)0x4);

            Stream.Write((short)tiles.Length);

            foreach (var tile in tiles)
            {
                Stream.Write((byte)tile.Number);

                if (tile.Flipped)
                {
                    var hand = tile.Dimensions.GetHandArea();

                    if (hand < 0 || players.IsPublic(hand) || players.GetPlayer(hand) == to ||
                        game.SpectatorVision && players.IsSpectator(to))
                    {
                        Stream.Write((byte)tile.Value);
                    }
                    else
                    {
                        Stream.Write((byte)0);
                    }
                }
                else
                {
                    Stream.Write((byte)0);
                }

                Stream.Write((short)tile.Position.Y);
                Stream.Write((short)tile.Position.X);
                Stream.Write((byte)tile.StackLevel);
                Stream.Write((byte)tile.Direction);

                Stream.Write(tile.Flipped ? (byte)0x10 : (byte)0x0);
            }
        }
    }

    public sealed class MahjongTileInfo : Packet
    {
        public MahjongTileInfo(MahjongTile tile, Mobile to) : base(0xDA)
        {
            var game = tile.Game;
            var players = game.Players;

            EnsureCapacity(18);

            Stream.Write(tile.Game.Serial);
            Stream.Write((byte)0);
            Stream.Write((byte)0x3);

            Stream.Write((byte)tile.Number);

            if (tile.Flipped)
            {
                var hand = tile.Dimensions.GetHandArea();

                if (hand < 0 || players.IsPublic(hand) || players.GetPlayer(hand) == to ||
                    game.SpectatorVision && players.IsSpectator(to))
                {
                    Stream.Write((byte)tile.Value);
                }
                else
                {
                    Stream.Write((byte)0);
                }
            }
            else
            {
                Stream.Write((byte)0);
            }

            Stream.Write((short)tile.Position.Y);
            Stream.Write((short)tile.Position.X);
            Stream.Write((byte)tile.StackLevel);
            Stream.Write((byte)tile.Direction);

            Stream.Write(tile.Flipped ? (byte)0x10 : (byte)0x0);
        }
    }

    public sealed class MahjongRelieve : Packet
    {
        public MahjongRelieve(Serial game) : base(0xDA)
        {
            EnsureCapacity(9);

            Stream.Write(game);
            Stream.Write((byte)0);
            Stream.Write((byte)0x1A);
        }
    }
}
