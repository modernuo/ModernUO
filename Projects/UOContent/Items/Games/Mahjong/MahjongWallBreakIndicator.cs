namespace Server.Engines.Mahjong
{
    public class MahjongWallBreakIndicator
    {
        public MahjongWallBreakIndicator(MahjongGame game, Point2D position)
        {
            Game = game;
            Position = position;
        }

        public MahjongWallBreakIndicator(MahjongGame game, IGenericReader reader)
        {
            Game = game;

            var version = reader.ReadInt();

            Position = reader.ReadPoint2D();
        }

        public MahjongGame Game { get; }

        public Point2D Position { get; private set; }

        public MahjongPieceDim Dimensions => GetDimensions(Position);

        public static MahjongPieceDim GetDimensions(Point2D position) => new(position, 20, 20);

        public void Move(Point2D position)
        {
            var dim = GetDimensions(position);

            if (!dim.IsValid())
            {
                return;
            }

            Position = position;

            Game.Players.SendGeneralPacket(true, true);
        }

        public void Save(IGenericWriter writer)
        {
            writer.Write(0); // version

            writer.Write(Position);
        }
    }
}
