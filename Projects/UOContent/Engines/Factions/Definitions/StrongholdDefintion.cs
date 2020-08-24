namespace Server.Factions
{
    public class StrongholdDefinition
    {
        public StrongholdDefinition(Rectangle2D[] area, Point3D joinStone, Point3D factionStone, Point3D[] monoliths)
        {
            Area = area;
            JoinStone = joinStone;
            FactionStone = factionStone;
            Monoliths = monoliths;
        }

        public Rectangle2D[] Area { get; }

        public Point3D JoinStone { get; }

        public Point3D FactionStone { get; }

        public Point3D[] Monoliths { get; }
    }
}
