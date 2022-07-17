namespace Server.Engines.Mahjong;

public struct MahjongPieceDim
{
    public Point2D Position { get; }

    public int Width { get; }

    public int Height { get; }

    public MahjongPieceDim(Point2D position, int width, int height)
    {
        Position = position;
        Width = width;
        Height = height;
    }

    public bool IsValid() =>
        Position.X >= 0 && Position.Y >= 0 && Position.X + Width <= 670 && Position.Y + Height <= 670;

    public bool IsOverlapping(MahjongPieceDim dim) =>
        Position.X < dim.Position.X + dim.Width && Position.Y < dim.Position.Y + dim.Height &&
        Position.X + Width > dim.Position.X && Position.Y + Height > dim.Position.Y;

    public int GetHandArea()
    {
        if (Position.X + Width > 150 && Position.X < 520 && Position.Y < 35)
        {
            return 0;
        }

        if (Position.X + Width > 635 && Position.Y + Height > 150 && Position.Y < 520)
        {
            return 1;
        }

        if (Position.X + Width > 150 && Position.X < 520 && Position.Y + Height > 635)
        {
            return 2;
        }

        if (Position.X < 35 && Position.Y + Height > 150 && Position.Y < 520)
        {
            return 3;
        }

        return -1;
    }
}
