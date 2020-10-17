using Server.Network;

namespace Server.Engines.Mahjong
{
    public delegate void OnMahjongPacketReceive(MahjongGame game, NetState state, ref BufferReader reader);

    public sealed class MahjongPacketHandlers
    {
        private static readonly OnMahjongPacketReceive[] m_SubCommandDelegates = new OnMahjongPacketReceive[0x100];

        public static void RegisterSubCommand(int subCmd, OnMahjongPacketReceive onReceive)
        {
            m_SubCommandDelegates[subCmd] = onReceive;
        }

        public static OnMahjongPacketReceive GetSubCommandDelegate(int cmd)
        {
            if (cmd >= 0 && cmd < 0x100)
            {
                return m_SubCommandDelegates[cmd];
            }

            return null;
        }

        public static void Initialize()
        {
            PacketHandlers.Register(0xDA, 0, true, OnPacket);

            RegisterSubCommand(0x6, ExitGame);
            RegisterSubCommand(0xA, GivePoints);
            RegisterSubCommand(0xB, RollDice);
            RegisterSubCommand(0xC, BuildWalls);
            RegisterSubCommand(0xD, ResetScores);
            RegisterSubCommand(0xF, AssignDealer);
            RegisterSubCommand(0x10, OpenSeat);
            RegisterSubCommand(0x11, ChangeOption);
            RegisterSubCommand(0x15, MoveWallBreakIndicator);
            RegisterSubCommand(0x16, TogglePublicHand);
            RegisterSubCommand(0x17, MoveTile);
            RegisterSubCommand(0x18, MoveDealerIndicator);
        }

        public static void OnPacket(NetState state,ref BufferReader reader)
        {
            var game = World.FindItem(reader.ReadUInt32()) as MahjongGame;

            game?.Players.CheckPlayers();

            reader.ReadByte();

            int cmd = reader.ReadByte();

            var onReceive = GetSubCommandDelegate(cmd);

            if (onReceive != null)
            {
                onReceive(game, state, ref reader);
            }
            else
            {
                reader.Trace(state);
            }
        }

        private static MahjongPieceDirection GetDirection(int value)
        {
            return value switch
            {
                0 => MahjongPieceDirection.Up,
                1 => MahjongPieceDirection.Left,
                2 => MahjongPieceDirection.Down,
                _ => MahjongPieceDirection.Right
            };
        }

        private static MahjongWind GetWind(int value)
        {
            return value switch
            {
                0 => MahjongWind.North,
                1 => MahjongWind.East,
                2 => MahjongWind.South,
                _ => MahjongWind.West
            };
        }

        public static void ExitGame(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game == null)
            {
                return;
            }

            var from = state.Mobile;

            game.Players.LeaveGame(from);
        }

        public static void GivePoints(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGamePlayer(state.Mobile) != true)
            {
                return;
            }

            int to = reader.ReadByte();
            var amount = reader.ReadInt32();

            game.Players.TransferScore(state.Mobile, to, amount);
        }

        public static void RollDice(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGamePlayer(state.Mobile) != true)
            {
                return;
            }

            game.Dices.RollDices(state.Mobile);
        }

        public static void BuildWalls(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGameDealer(state.Mobile) != true)
            {
                return;
            }

            game.ResetWalls(state.Mobile);
        }

        public static void ResetScores(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGameDealer(state.Mobile) != true)
            {
                return;
            }

            game.Players.ResetScores(MahjongGame.BaseScore);
        }

        public static void AssignDealer(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGameDealer(state.Mobile) != true)
            {
                return;
            }

            int position = reader.ReadByte();

            game.Players.AssignDealer(position);
        }

        public static void OpenSeat(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGameDealer(state.Mobile) != true)
            {
                return;
            }

            int position = reader.ReadByte();

            if (game.Players.GetPlayer(position) == state.Mobile)
            {
                return;
            }

            game.Players.OpenSeat(position);
        }

        public static void ChangeOption(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGameDealer(state.Mobile) != true)
            {
                return;
            }

            reader.ReadInt16();
            reader.ReadByte();

            int options = reader.ReadByte();

            game.ShowScores = (options & 0x1) != 0;
            game.SpectatorVision = (options & 0x2) != 0;
        }

        public static void MoveWallBreakIndicator(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGameDealer(state.Mobile) != true)
            {
                return;
            }

            int y = reader.ReadInt16();
            int x = reader.ReadInt16();

            game.WallBreakIndicator.Move(new Point2D(x, y));
        }

        public static void TogglePublicHand(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGamePlayer(state.Mobile) != true)
            {
                return;
            }

            reader.ReadInt16();
            reader.ReadByte();

            var publicHand = reader.ReadBoolean();

            game.Players.SetPublic(game.Players.GetPlayerIndex(state.Mobile), publicHand);
        }

        public static void MoveTile(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGamePlayer(state.Mobile) != true)
            {
                return;
            }

            int number = reader.ReadByte();

            if (number < 0 || number >= game.Tiles.Length)
            {
                return;
            }

            reader.ReadByte(); // Current direction

            var direction = GetDirection(reader.ReadByte());

            reader.ReadByte();

            var flip = reader.ReadBoolean();

            reader.ReadInt16(); // Current Y
            reader.ReadInt16(); // Current X

            reader.ReadByte();

            int y = reader.ReadInt16();
            int x = reader.ReadInt16();

            reader.ReadByte();

            game.Tiles[number].Move(new Point2D(x, y), direction, flip, game.Players.GetPlayerIndex(state.Mobile));
        }

        public static void MoveDealerIndicator(MahjongGame game, NetState state, ref BufferReader reader)
        {
            if (game?.Players.IsInGameDealer(state.Mobile) != true)
            {
                return;
            }

            var direction = GetDirection(reader.ReadByte());

            var wind = GetWind(reader.ReadByte());

            int y = reader.ReadInt16();
            int x = reader.ReadInt16();

            game.DealerIndicator.Move(new Point2D(x, y), direction, wind);
        }
    }
}
