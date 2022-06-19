namespace Server.PathAlgorithms
{
    public abstract class PathAlgorithm
    {
        private static readonly Direction[] m_CalcDirections =
        {
            Direction.Up,
            Direction.North,
            Direction.Right,
            Direction.West,
            Direction.North,
            Direction.East,
            Direction.Left,
            Direction.South,
            Direction.Down
        };

        public abstract bool CheckCondition(Mobile m, Map map, Point3D start, Point3D goal);
        public abstract Direction[] Find(Mobile m, Map map, Point3D start, Point3D goal);

        public Direction GetDirection(int xSource, int ySource, int xDest, int yDest)
        {
            var x = xDest + 1 - xSource;
            var y = yDest + 1 - ySource;
            var v = y * 3 + x;

            if (v is < 0 or >= 9)
            {
                return Direction.North;
            }

            return m_CalcDirections[v];
        }
    }
}
