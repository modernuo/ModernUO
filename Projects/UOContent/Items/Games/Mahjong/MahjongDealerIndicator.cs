namespace Server.Engines.Mahjong
{
    public class MahjongDealerIndicator
    {
        public MahjongDealerIndicator(MahjongGame game, Point2D position, MahjongPieceDirection direction, MahjongWind wind)
        {
            Game = game;
            Position = position;
            Direction = direction;
            Wind = wind;
        }

        public MahjongDealerIndicator(MahjongGame game, IGenericReader reader)
        {
            Game = game;

            var version = reader.ReadInt();

            Position = reader.ReadPoint2D();
            Direction = (MahjongPieceDirection)reader.ReadInt();
            Wind = (MahjongWind)reader.ReadInt();
        }

        public MahjongGame Game { get; }

        public Point2D Position { get; private set; }

        public MahjongPieceDirection Direction { get; private set; }

        public MahjongWind Wind { get; private set; }

        public MahjongPieceDim Dimensions => GetDimensions(Position, Direction);

        public static MahjongPieceDim GetDimensions(Point2D position, MahjongPieceDirection direction)
        {
            if (direction == MahjongPieceDirection.Up || direction == MahjongPieceDirection.Down)
            {
                return new MahjongPieceDim(position, 40, 20);
            }

            return new MahjongPieceDim(position, 20, 40);
        }

        public void Move(Point2D position, MahjongPieceDirection direction, MahjongWind wind)
        {
            var dim = GetDimensions(position, direction);

            if (!dim.IsValid())
            {
                return;
            }

            Position = position;
            Direction = direction;
            Wind = wind;

            Game.Players.SendGeneralPacket(true, true);
        }

        public void Save(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(Position);
            writer.Write((int)Direction);
            writer.Write((int)Wind);
        }
    }
}
